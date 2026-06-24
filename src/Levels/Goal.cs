using Godot;

namespace Capyball;

/// <summary>
/// The goal: a glowing vertical gate you roll <b>through from either side</b>
/// to finish the level. Two chunky posts, a crossbar banner, a translucent glow
/// panel filling the opening, and spinning rings. A tall wide box trigger spans
/// the opening — entering it from any horizontal direction wins.
/// </summary>
public partial class Goal : Area3D
{
    [Export] public string NextLevelId = "";
    [Export] public float Width = 4.5f;   // inner opening width
    [Export] public float Height = 4.0f;  // inner opening height
    public bool Reached { get; private set; }

    private MeshInstance3D _ringA;
    private MeshInstance3D _ringB;
    private Light3D _light;
    private GpuParticles3D _sparkle;

    public override void _Ready()
    {
        float postH = Height + 1.0f;
        float postSize = 0.6f;
        float halfW = Width * 0.5f + postSize * 0.5f;

        // Trigger volume: a tall box spanning the opening, thin along the roll axis
        // so the ball passes cleanly through from either side.
        var col = new CollisionShape3D();
        col.Shape = new BoxShape3D { Size = new Vector3(Width, Height, 1.6f) };
        AddChild(col);
        Monitoring = true;
        Monitorable = false;

        var beamMat = Assets.Material("goal_beam");
        var ringMatA = Assets.Material("goal_ring_a");
        var ringMatB = Assets.Material("goal_ring_b");
        var postMat = Assets.Material("goal_ring_b");

        // Two glowing posts.
        AddPost(new Vector3(-halfW, postH * 0.5f, 0), new Vector3(postSize, postH, postSize), postMat);
        AddPost(new Vector3(halfW, postH * 0.5f, 0), new Vector3(postSize, postH, postSize), postMat);

        // Crossbar banner across the top.
        var bar = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(Width + postSize * 2f, postSize, postSize) },
            Position = new Vector3(0, Height + postSize * 0.5f, 0),
        };
        bar.MaterialOverride = beamMat;
        AddChild(bar);

        // Translucent glow panel filling the opening.
        var panel = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(Width, Height, 0.15f) },
            Position = new Vector3(0, Height * 0.5f, 0),
        };
        var panelMat = (StandardMaterial3D)Assets.Material("goal_beam").Duplicate();
        panelMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        panelMat.AlbedoColor = new Color(0.5f, 1.0f, 0.7f, 0.30f);
        panelMat.EmissionEnergyMultiplier = 2.2f;
        panel.MaterialOverride = panelMat;
        AddChild(panel);

        // Spinning rings floating in the opening.
        _ringA = Procedural.GlowRing(1.4f, ringMatA, 0.16f);
        _ringA.Position = new Vector3(0, Height * 0.5f, 0);
        AddChild(_ringA);

        _ringB = Procedural.GlowRing(1.0f, ringMatB, 0.12f);
        _ringB.Position = new Vector3(0, Height * 0.5f, 0);
        AddChild(_ringB);

        // Point light for local glow.
        _light = new OmniLight3D
        {
            LightColor = new Color(0.6f, 1f, 0.7f),
            LightEnergy = 2.5f,
            OmniRange = 12f,
            Position = new Vector3(0, Height * 0.5f, 0),
        };
        AddChild(_light);

        // Ambient sparkle.
        _sparkle = MakeSparkle();
        AddChild(_sparkle);

        BodyEntered += OnBody;
    }

    private void AddPost(Vector3 pos, Vector3 size, Material mat)
    {
        var mi = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = pos };
        mi.MaterialOverride = mat;
        AddChild(mi);
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
