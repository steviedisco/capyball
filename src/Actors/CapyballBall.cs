using Godot;

namespace Capyball;

/// <summary>
/// The hero: a capybara encased in a glowing ball. Super Monkey Ball style
/// physics — the player tilts the world's gravity, the ball rolls. Boost adds
/// an impulse along facing, jump gives a satisfying vertical pop with squash.
/// </summary>
public partial class CapyballBall : RigidBody3D
{
    // Tuning ---------------------------------------------------------------
    [Export] public float MaxMoveForce = 26f;
    [Export] public float Acceleration = 14f;
    [Export] public float MaxSpeed = 19f;
    [Export] public float BoostImpulse = 11f;
    [Export] public float JumpImpulse = 8.5f;
    [Export] public float AirControl = 0.35f;
    [Export] public float GroundCheckDist = 0.62f;
    [Export] public float GroundCheckRadius = 0.42f;

    // State ----------------------------------------------------------------
    public bool Grounded { get; private set; }
    public Vector3 Facing { get; private set; } = Vector3.Forward;
    public float Speed => LinearVelocity.Length();

    private Node3D _model;
    private MeshInstance3D _shell;
    private CameraFollow _cam;
    private GpuParticles3D _trail;
    private GpuParticles3D _boostFx;

    private float _targetTiltX, _targetTiltZ;
    private float _squash = 1f;
    private Vector3 _lastPos;
    private float _rollTimer;
    private float _boostCooldown;
    private bool _boostActive;
    private float _jumpWindow;
    private Vector3 _impactPending;
    private bool _landedThisFrame;

    public override void _Ready()
    {
        ContactMonitor = true;
        MaxContactsReported = 12;
        BodyEntered += OnBodyEntered;

        GravityScale = 1.6f;
        Mass = 1.2f;
        LinearDamp = 0.4f;
        AngularDamp = 1.5f;
        CustomIntegrator = false;
        PhysicsMaterialOverride = new PhysicsMaterial { Friction = 0.9f, Bounce = 0.05f };

        // Collision shape — a sphere.
        var col = new CollisionShape3D();
        var sphere = new SphereShape3D { Radius = 0.6f };
        col.Shape = sphere;
        AddChild(col);

        // Visual shell — disk-based material.
        _shell = Procedural.BallShell(0.62f, Assets.MaterialMutable("ball_shell"));
        AddChild(_shell);

        // Capybara model — authored scene loaded from disk.
        _model = Assets.Capybara().Instantiate<Node3D>();
        _model.Scale = Vector3.One * 0.55f;
        AddChild(_model);

        // Trail particles — the "I'm going fast" tell.
        _trail = MakeTrail();
        AddChild(_trail);

        // Boost flame jet.
        _boostFx = MakeBoostFx();
        AddChild(_boostFx);
        _boostFx.Emitting = false;

        _lastPos = GlobalPosition;
    }

    public void BindCamera(CameraFollow cam) => _cam = cam;

    public override void _PhysicsProcess(double deltaDouble)
    {
        float dt = (float)deltaDouble;

        Grounded = CheckGround();
        Vector2 input = ReadInput();
        Vector3 wish = WishDir(input);

        // Camera-relative steering.
        Vector3 camF = _cam != null ? _cam.ForwardFlat : Vector3.Forward;
        Vector3 camR = _cam != null ? _cam.RightFlat : Vector3.Right;
        Vector3 moveDir = (camF * wish.Z + camR * wish.X);
        if (moveDir.LengthSquared() > 1f) moveDir = moveDir.Normalized();

        ApplyMovement(moveDir, dt);
        UpdateFacing(moveDir, dt);

        // Boost + jump.
        _boostCooldown -= dt;
        if (Input.IsActionJustPressed("boost") && _boostCooldown <= 0 && Grounded)
        {
            ApplyBoost();
            _boostCooldown = 0.9f;
        }
        if (Input.IsActionJustPressed("jump"))
            TryJump();

        // Rolling audio cadence.
        _rollTimer -= dt;
        if (_rollTimer <= 0 && Grounded && Speed > 2f)
        {
            Synth.Instance?.Roll(Mathf.InverseLerp(2f, MaxSpeed, Speed));
            _rollTimer = Mathf.Lerp(0.18f, 0.06f, Mathf.InverseLerp(2f, MaxSpeed, Speed));
        }

        Animate(dt);
        UpdateTrail();

        // Resolve queued impacts (squelch/landing FX).
        if (_landedThisFrame)
        {
            _landedThisFrame = false;
            float strength = Mathf.Clamp(LinearVelocity.Length() / MaxSpeed, 0.2f, 1f);
            Squash(0.78f, 1.18f);
            Fx.Instance?.Shake(strength * 0.35f);
            Fx.Instance?.Burst(GlobalPosition, Vector3.Up, Palette.BallRim, strength * 1.4f);
            Synth.Instance?.Land(strength);
        }

        // Speed lines beyond threshold handled in trail intensity.
    }

    private Vector2 ReadInput()
    {
        Vector2 i = Vector2.Zero;
        if (Input.IsActionPressed("move_left")) i.X -= 1;
        if (Input.IsActionPressed("move_right")) i.X += 1;
        if (Input.IsActionPressed("move_forward")) i.Y += 1;
        if (Input.IsActionPressed("move_back")) i.Y -= 1;
        return i;
    }

    private static Vector3 WishDir(Vector2 i) => new(i.X, 0, i.Y);

    private void ApplyMovement(Vector3 dir, float dt)
    {
        float control = Grounded ? 1f : AirControl;
        Vector3 force = dir * Acceleration * control;
        if (force.LengthSquared() > 0f)
            ApplyCentralForce(force);

        // Hard cap horizontal speed for arcade feel.
        Vector3 h = new(LinearVelocity.X, 0, LinearVelocity.Z);
        if (h.Length() > MaxSpeed)
        {
            Vector3 clamped = h.Normalized() * MaxSpeed;
            LinearVelocity = new Vector3(clamped.X, LinearVelocity.Y, clamped.Z);
        }
    }

    private void UpdateFacing(Vector3 dir, float dt)
    {
        if (dir.LengthSquared() > 0.05f)
            Facing = Facing.Lerp(dir.Normalized(), 1f - Mathf.Exp(-12f * dt));
    }

    private void ApplyBoost()
    {
        Vector3 dir = new(Facing.X, 0.2f, Facing.Z);
        if (dir.LengthSquared() > 0f) dir = dir.Normalized();
        ApplyCentralImpulse(dir * BoostImpulse);
        _boostActive = true;
        _boostFx.Emitting = true;
        Squash(1.18f, 0.85f); // stretch along facing
        Fx.Instance?.Shake(0.18f);
        Fx.Instance?.Burst(GlobalPosition - dir, -dir, Palette.BoostFlame, 1.2f);
        Synth.Instance?.Boost();

        var tw = CreateTween();
        tw.TweenInterval(0.45);
        tw.TweenCallback(Callable.From(() => _boostActive = false));
    }

    private void TryJump()
    {
        if (Grounded)
        {
            ApplyCentralImpulse(Vector3.Up * JumpImpulse);
            Grounded = false;
            Squash(0.82f, 1.2f);
            Fx.Instance?.Burst(GlobalPosition + Vector3.Down * 0.4f, Vector3.Down, Palette.BallRim, 1.0f);
            Synth.Instance?.Bump(0.6f);
        }
    }

    private bool CheckGround()
    {
        var space = GetWorld3D().DirectSpaceState;
        var shape = new SphereShape3D { Radius = GroundCheckRadius };
        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = shape,
            Transform = new Transform3D(Basis.Identity, GlobalPosition + Vector3.Down * GroundCheckDist),
            CollideWithBodies = true,
            CollideWithAreas = false,
            Exclude = new Godot.Collections.Array<Rid>(),
        };
        var hits = space.IntersectShape(query);
        return hits.Count > 0;
    }

    private void OnBodyEntered(Node body)
    {
        // A light bump FX on collision.
        float rel = (LinearVelocity).Length();
        if (rel > 4f)
        {
            Fx.Instance?.Shake(Mathf.Clamp(rel / 30f, 0f, 0.3f));
            Synth.Instance?.Bump(Mathf.Clamp(rel / 20f, 0f, 1f));
            // Surface normal approx from direction to body.
            Vector3 otherPos = body is Node3D b ? b.GlobalPosition : GlobalPosition + Vector3.Up;
            Vector3 n = GlobalPosition - otherPos;
            if (n.LengthSquared() > 0f) n = n.Normalized();
            Fx.Instance?.Burst(GlobalPosition, n, Palette.BallRim, 0.8f);
        }
    }

    // Mark landing when downward vertical velocity is arrested on contact.
    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        bool wasFalling = state.LinearVelocity.Y < -2f;
        for (int i = 0; i < state.GetContactCount(); i++)
        {
            Vector3 n = state.GetContactLocalNormal(i);
            if (n.Y > 0.6f && wasFalling)
            {
                _landedThisFrame = true;
            }
        }
    }

    // --- Animation / juice -------------------------------------------------

    private void Animate(float dt)
    {
        // Roll the ball shell to match velocity (visual only).
        Vector3 v = LinearVelocity;
        if (v.LengthSquared() > 0.01f)
        {
            Vector3 axis = v.Cross(Vector3.Up).Normalized();
            float ang = v.Length() / 0.6f * dt;
            if (axis.LengthSquared() > 0.01f)
                _shell.Rotate(axis.Normalized(), ang);
        }

        // Squash recovery (ease back to 1).
        _squash = Mathf.Lerp(_squash, 1f, 1f - Mathf.Exp(-14f * dt));
        _shell.Scale = new Vector3(2f - _squash, _squash, 2f - _squash);

        // Face the model toward travel direction and bob it.
        if (_model != null)
        {
            Vector3 f = Facing;
            if (f.LengthSquared() > 0.01f)
            {
                float targetYaw = Mathf.Atan2(f.X, f.Z);
                float cur = _model.Rotation.Y;
                float y = Mathf.LerpAngle(cur, targetYaw, 1f - Mathf.Exp(-14f * dt));
                _model.Rotation = new Vector3(_model.Rotation.X, y, _model.Rotation.Z);
            }
            float bob = Mathf.Sin(Time.GetTicksMsec() * 0.012f) * 0.04f;
            _model.Position = new Vector3(0, bob, 0);
        }
    }

    private void Squash(float y, float xz)
    {
        _squash = Mathf.Clamp(Mathf.Lerp(y, 1f, 0.0f), 0.4f, 1.6f);
        // Blend to desired immediately, easing back handled in Animate.
        _shell.Scale = new Vector3(xz, _squash, xz);
    }

    private void UpdateTrail()
    {
        if (_trail == null) return;
        float t = Mathf.Clamp(Mathf.InverseLerp(7f, MaxSpeed, Speed), 0f, 1f);
        var mat = (ParticleProcessMaterial)_trail.ProcessMaterial;
        mat.InitialVelocityMin = Mathf.Lerp(0.5f, 3.5f, t);
        mat.InitialVelocityMax = Mathf.Lerp(1.0f, 6.0f, t);
        _trail.Amount = Mathf.Max(1, (int)Mathf.Lerp(8, 40, t));
        var qmat = (StandardMaterial3D)((QuadMesh)_trail.DrawPass1).Material;
        qmat.EmissionEnergyMultiplier = Mathf.Lerp(1.0f, 4.0f, t);
        qmat.AlbedoColor = new Color(1, 1, 1, Mathf.Lerp(0.3f, 1f, t));
        _trail.Emitting = Speed > 4f;
    }

    private GpuParticles3D MakeTrail()
    {
        var p = new GpuParticles3D
        {
            Amount = 20,
            Lifetime = 0.45,
            OneShot = false,
            Emitting = false,
            FixedFps = 60,
        };
        // Particle process material + draw material loaded from disk (mutable copies,
        // since UpdateTrail() mutates them per frame).
        p.ProcessMaterial = (ParticleProcessMaterial)Assets.ParticleMaterial("trail_particle").Duplicate();
        var quad = new QuadMesh { Size = new Vector2(0.3f, 0.3f) };
        quad.Material = Assets.MaterialMutable("trail");
        p.DrawPass1 = quad;
        return p;
    }

    private GpuParticles3D MakeBoostFx()
    {
        var p = new GpuParticles3D
        {
            Amount = 40,
            Lifetime = 0.4,
            OneShot = false,
            Emitting = false,
            FixedFps = 60,
            Position = new Vector3(0, 0, -0.2f),
        };
        p.ProcessMaterial = Assets.ParticleMaterial("boost_flame_particle");
        var quad = new QuadMesh { Size = new Vector2(0.28f, 0.28f) };
        quad.Material = Assets.Material("boost_flame");
        p.DrawPass1 = quad;
        return p;
    }
}
