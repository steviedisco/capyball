using Godot;

namespace Capyball;

/// <summary>
/// A bouncy bumper — like a pinball bumper. Knocks the ball away on contact with
/// a big particle pop, flash, and screen-shake. Pure juice.
/// </summary>
public partial class Bumper : Area3D
{
    [Export] public float Force = 14f;
    [Export] public Color Tint = Palette.HotPink;

    private MeshInstance3D _core;
    private Light3D _light;
    private float _flash;

    public override void _Ready()
    {
        var col = new CollisionShape3D();
        col.Shape = new CylinderShape3D { Radius = 0.9f, Height = 1.6f };
        AddChild(col);

        _core = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.85f, BottomRadius = 0.85f, Height = 1.4f },
        };
        // Tinted per-instance and mutable (flashed in _Process).
        _core.MaterialOverride = Assets.MaterialTinted("bumper_core", Tint);
        AddChild(_core);

        var ring = Procedural.GlowRing(0.95f, Assets.Material("bumper_ring"), 0.12f);
        ring.Position = new Vector3(0, 0.8f, 0);
        AddChild(ring);

        _light = new OmniLight3D
        {
            LightColor = Tint,
            LightEnergy = 1.5f,
            OmniRange = 6f,
        };
        AddChild(_light);

        BodyEntered += OnBody;
    }

    public override void _Process(double delta)
    {
        _flash = Mathf.Max(0, _flash - (float)delta * 4f);
        if (_core.MaterialOverride is StandardMaterial3D m)
            m.EmissionEnergyMultiplier = 0.8f + _flash * 3f;
        _core.Scale = new Vector3(1f + _flash * 0.3f, 1f - _flash * 0.2f, 1f + _flash * 0.3f);
    }

    private void OnBody(Node body)
    {
        if (body is not CapyballBall ball) return;
        Vector3 dir = (ball.GlobalPosition - GlobalPosition);
        dir.Y = 0.2f;
        if (dir.LengthSquared() > 0f) dir = dir.Normalized();
        ball.ApplyCentralImpulse(dir * Force);
        _flash = 1f;
        Fx.Instance?.Burst(GlobalPosition + Vector3.Up * 0.6f, dir, Tint, 1.5f);
        Fx.Instance?.Shake(0.35f);
        Fx.Instance?.Freeze(0.06f, 0.4f);
        Synth.Instance?.Bump(1f);
    }
}
