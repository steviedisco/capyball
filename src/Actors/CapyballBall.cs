using Godot;

namespace Capyball;

/// <summary>
/// The hero: a capybara encased in a glowing ball. Authentic Super Monkey Ball
/// feel — the player does NOT push the ball. Instead, input <b>tilts gravity</b>
/// (up to <see cref="MaxTiltDeg"/>) so the ball rolls downhill via real physics.
/// Tilting the opposite way decelerates, then reverses, the roll — the signature
/// momentum/commitment feel. No jump, no boost; the ball is pure tilt + inertia.
/// </summary>
public partial class CapyballBall : RigidBody3D
{
    // Tilt feel (the knobs to tune during playtest) ------------------------
    [Export] public float MaxTiltDeg = 24f;       // how steep the gravity tilt can get
    [Export] public float TiltSpeed = 9f;         // how quickly tilt responds to input
    [Export] public float GravityScale = 2.0f;    // overall pull strength
    [Export] public float LinearDamp = 0.15f;     // mild rolling resistance
    [Export] public float MaxSpeed = 22f;         // soft cap on horizontal speed
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

    // Current tilt (X = pitch forward/back, Z = roll left/right), in radians.
    private float _tiltX, _tiltZ;
    private float _squash = 1f;
    private float _rollTimer;
    private bool _landedThisFrame;

    public override void _Ready()
    {
        ContactMonitor = true;
        MaxContactsReported = 12;
        BodyEntered += OnBodyEntered;

        Mass = 1.2f;
        // We integrate gravity ourselves (tilted), so take over the step.
        CustomIntegrator = true;
        GravityScale = 0f; // handled in _IntegrateForces
        PhysicsMaterialOverride = new PhysicsMaterial { Friction = 0.85f, Bounce = 0.04f };

        // Collision shape — a sphere.
        var col = new CollisionShape3D();
        col.Shape = new SphereShape3D { Radius = 0.6f };
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
    }

    public void BindCamera(CameraFollow cam) => _cam = cam;

    public override void _PhysicsProcess(double deltaDouble)
    {
        float dt = (float)deltaDouble;

        Grounded = CheckGround();

        // Read input as a target tilt vector (camera-relative).
        Vector2 input = ReadInput();
        UpdateTilt(input, dt);

        // Rolling audio cadence.
        _rollTimer -= dt;
        if (_rollTimer <= 0 && Grounded && Speed > 2f)
        {
            Synth.Instance?.Roll(Mathf.InverseLerp(2f, MaxSpeed, Speed));
            _rollTimer = Mathf.Lerp(0.18f, 0.06f, Mathf.InverseLerp(2f, MaxSpeed, Speed));
        }

        Animate(dt);
        UpdateTrail();

        // Resolve queued landing FX.
        if (_landedThisFrame)
        {
            _landedThisFrame = false;
            float strength = Mathf.Clamp(LinearVelocity.Length() / MaxSpeed, 0.2f, 1f);
            Squash(0.78f, 1.18f);
            Fx.Instance?.Shake(strength * 0.35f);
            Fx.Instance?.Burst(GlobalPosition, Vector3.Up, Palette.BallRim, strength * 1.4f);
        }
    }

    private Vector2 ReadInput()
    {
        Vector2 i = Vector2.Zero;
        if (Input.IsActionPressed("move_left")) i.X -= 1;
        if (Input.IsActionPressed("move_right")) i.X += 1;
        if (Input.IsActionPressed("move_forward")) i.Y += 1;
        if (Input.IsActionPressed("move_back")) i.Y -= 1;
        return i.LengthSquared() > 1f ? i.Normalized() : i;
    }

    /// <summary>Smooth the tilt toward the input-derived target. Input is in the
    /// camera's frame: forward/back tilts gravity along the camera's forward axis,
    /// left/right along the camera's right axis.</summary>
    private void UpdateTilt(Vector2 input, float dt)
    {
        // Target tilt in radians.
        float maxTilt = Mathf.DegToRad(MaxTiltDeg);
        float targetX = input.Y * maxTilt; // forward input -> gravity tilts forward
        float targetZ = -input.X * maxTilt; // right input -> gravity tilts right

        float k = 1f - Mathf.Exp(-TiltSpeed * dt);
        _tiltX = Mathf.Lerp(_tiltX, targetX, k);
        _tiltZ = Mathf.Lerp(_tiltZ, targetZ, k);
    }

    /// <summary>The tilted gravity vector, built directly in the camera's frame so
    /// "forward" always means "away from the camera". The down component stays
    /// constant (full gravity pulls the ball onto the surface) and a horizontal
    /// component appears in the tilt direction — exactly like rolling down a slope,
    /// where the steeper the tilt the harder gravity pulls you sideways.</summary>
    private Vector3 TiltedGravity()
    {
        float g = (float)ProjectSettings.GetSetting("physics/3d/default_gravity", 9.8) * GravityScale;

        // Build the horizontal lean in the camera frame. _tiltX is the forward/back
        // tilt (input.Y), _tiltZ is the left/right tilt (-input.X). tan(angle) gives
        // the slope ratio: at maxTilt (~24°) this is ~0.44, a strong but controllable lean.
        Vector3 camF = _cam != null ? _cam.ForwardFlat : Vector3.Forward;
        Vector3 camR = _cam != null ? _cam.RightFlat : Vector3.Right;
        Vector3 lean = camF * Mathf.Tan(_tiltX) + camR * Mathf.Tan(-_tiltZ);

        // Full downward gravity keeps the ball glued to the floor; the lean is the
        // "slope" component that makes it roll.
        return Vector3.Down * g + lean * g;
    }

    // Custom integration: apply tilted gravity + light damping, detect landings.
    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        // Apply tilted gravity as a force over this step.
        Vector3 g = TiltedGravity();
        state.ApplyCentralForce(g * state.Step);

        // Mild linear damping for stability / so it doesn't accelerate forever.
        state.LinearVelocity *= Mathf.Max(0f, 1f - LinearDamp * (float)state.Step);

        // Soft horizontal speed cap (arcade feel).
        Vector3 h = new(state.LinearVelocity.X, 0, state.LinearVelocity.Z);
        if (h.Length() > MaxSpeed)
        {
            Vector3 clamped = h.Normalized() * MaxSpeed;
            state.LinearVelocity = new Vector3(clamped.X, state.LinearVelocity.Y, clamped.Z);
        }

        // Landing detection: was falling, now touching an upward-facing surface.
        bool wasFalling = state.LinearVelocity.Y < -2f;
        for (int i = 0; i < state.GetContactCount(); i++)
        {
            Vector3 n = state.GetContactLocalNormal(i);
            if (n.Y > 0.6f && wasFalling)
                _landedThisFrame = true;
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
        float rel = LinearVelocity.Length();
        if (rel > 4f)
        {
            Fx.Instance?.Shake(Mathf.Clamp(rel / 30f, 0f, 0.3f));
            Synth.Instance?.Bump(Mathf.Clamp(rel / 20f, 0f, 1f));
            Vector3 otherPos = body is Node3D b ? b.GlobalPosition : GlobalPosition + Vector3.Up;
            Vector3 n = GlobalPosition - otherPos;
            if (n.LengthSquared() > 0f) n = n.Normalized();
            Fx.Instance?.Burst(GlobalPosition, n, Palette.BallRim, 0.8f);
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

        // Facing follows actual horizontal velocity (where the ball is going).
        Vector3 horiz = new(LinearVelocity.X, 0, LinearVelocity.Z);
        if (horiz.LengthSquared() > 1f)
            Facing = Facing.Lerp(horiz.Normalized(), 1f - Mathf.Exp(-12f * dt));

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
        _squash = Mathf.Clamp(y, 0.4f, 1.6f);
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
}
