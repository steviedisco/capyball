using Godot;

namespace Capyball;

/// <summary>
/// The goal — a glowing goal ring you roll through. Big juicy payoff:
/// beam of light, spinning rings, ascending chime, confetti, slow-mo beat.
/// </summary>
public partial class Goal : Area3D
{
    [Export] public string NextLevelId = "";
    public bool Reached { get; private set; }

    private MeshInstance3D _beam;
    private MeshInstance3D _ringA;
    private MeshInstance3D _ringB;
    private GpuParticles3D _sparkle;
    private Light3D _light;

    public override void _Ready()
    {
        // Collision: a tall thin cylinder trigger.
        var col = new CollisionShape3D();
        col.Shape = new CylinderShape3D { Radius = 1.4f, Height = 5.0f };
        AddChild(col);

        // Glowing beam.
        var beamMesh = new CylinderMesh { TopRadius = 0.6f, BottomRadius = 0.9f, Height = 14f };
        beamMesh.Material = Assets.Material("goal_beam");
        _beam = new MeshInstance3D { Mesh = beamMesh, Position = new Vector3(0, 6, 0) };
        AddChild(_beam);

        // Spinning rings on the ground.
        _ringA = Procedural.GlowRing(1.4f, Assets.Material("goal_ring_a"), 0.16f);
        _ringA.Position = new Vector3(0, 0.1f, 0);
        AddChild(_ringA);

        _ringB = Procedural.GlowRing(1.0f, Assets.Material("goal_ring_b"), 0.12f);
        _ringB.Position = new Vector3(0, 0.15f, 0);
        AddChild(_ringB);

        // Point light for local glow.
        _light = new OmniLight3D
        {
            LightColor = new Color(0.6f, 1f, 0.7f),
            LightEnergy = 2.5f,
            OmniRange = 10f,
            Position = new Vector3(0, 1.5f, 0),
        };
        AddChild(_light);

        // Ambient sparkle.
        _sparkle = MakeSparkle();
        AddChild(_sparkle);

        BodyEntered += OnBody;
    }

    private GpuParticles3D MakeSparkle()
    {
        var p = new GpuParticles3D { Amount = 24, Lifetime = 1.2f, Position = new Vector3(0, 2, 0) };
        p.ProcessMaterial = Assets.ParticleMaterial("goal_sparkle_particle");
        var quad = new QuadMesh { Size = new Vector2(0.2f, 0.2f) };
        quad.Material = Assets.Material("goal_sparkle");
        p.DrawPass1 = quad;
        return p;
    }

    public override void _Process(double delta)
    {
        if (Reached) return;
        _ringA.RotateY((float)delta * 1.4f);
        _ringB.RotateY((float)delta * -2.2f);
        float pulse = 0.85f + Mathf.Sin(Time.GetTicksMsec() * 0.005f) * 0.15f;
        _beam.Scale = new Vector3(pulse, 1f, pulse);
        ((OmniLight3D)_light).LightEnergy = 2.0f + Mathf.Sin(Time.GetTicksMsec() * 0.006f) * 0.8f;
    }

    private void OnBody(Node body)
    {
        if (Reached || body is not CapyballBall) return;
        Reached = true;
        Synth.Instance?.Goal();
        Fx.Instance?.Freeze(0.18f, 0.25f);
        Fx.Instance?.Confetti(GlobalPosition + Vector3.Up * 1.5f);
        Fx.Instance?.Shake(0.5f);
        var lvl = GetParentOrNull<LevelScene>();
        lvl?.OnGoalReached(this);
    }
}
