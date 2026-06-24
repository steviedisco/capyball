using Godot;

namespace Capyball;

/// <summary>
/// A boost pad: a glowing chevron plate that fires the ball forward along its
/// facing when the ball rolls over it. With spin FX and a chunky whoosh.
/// </summary>
public partial class BoostPad : Area3D
{
    [Export] public Vector3 Direction = Vector3.Forward; // local-space launch dir
    [Export] public float Force = 16f;
    [Export] public Color Tint = Palette.BoostFlame;

    private MeshInstance3D _plate;
    private MeshInstance3D _chevron;
    private float _pulseT;

    public override void _Ready()
    {
        var col = new CollisionShape3D();
        col.Shape = new BoxShape3D { Size = new Vector3(2.6f, 0.4f, 2.6f) };
        AddChild(col);

        // Recessed glowing plate.
        var plateMesh = new BoxMesh { Size = new Vector3(2.4f, 0.2f, 2.4f) };
        plateMesh.Material = Procedural.Candy(Tint, emission: 1.2f, rough: 0.3f);
        _plate = new MeshInstance3D { Mesh = plateMesh, Position = new Vector3(0, 0.05f, 0) };
        AddChild(_plate);

        // Chevron arrows — a couple of thin glowing boxes angled forward.
        for (int i = 0; i < 3; i++)
        {
            var chev = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(1.4f, 0.06f, 0.35f) },
                Position = new Vector3(0, 0.16f, -0.7f + i * 0.6f),
            };
            chev.MaterialOverride = Procedural.Glow(Palette.BoostCore, 3f);
            chev.Rotation = new Vector3(0, 0, 0);
            if (i == 0) _chevron = chev;
            _plate.AddChild(chev);
        }

        BodyEntered += OnBody;
    }

    public override void _Process(double delta)
    {
        _pulseT += (float)delta;
        float p = 0.85f + Mathf.Sin(_pulseT * 6f) * 0.15f;
        if (_plate.MaterialOverride is StandardMaterial3D m)
            m.EmissionEnergyMultiplier = 1.0f + p;
        // March chevrons forward to imply motion.
        foreach (var c in _plate.GetChildren())
            if (c is MeshInstance3D mi)
            {
                float z = Mathf.PosMod(mi.Position.Z + (float)delta * 3f + 0.7f, 2.0f) - 0.7f;
                mi.Position = new Vector3(0, mi.Position.Y, z);
            }
    }

    private void OnBody(Node body)
    {
        if (body is not CapyballBall ball) return;
        Vector3 worldDir = GlobalTransform.Basis * Direction;
        worldDir = new Vector3(worldDir.X, 0.25f, worldDir.Z).Normalized();
        ball.ApplyCentralImpulse(worldDir * Force);
        Fx.Instance?.Burst(GlobalPosition + Vector3.Up * 0.4f, worldDir, Tint, 1.3f);
        Fx.Instance?.Shake(0.22f);
        Synth.Instance?.Whoosh();
    }
}
