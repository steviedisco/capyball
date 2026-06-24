using Godot;

namespace Capyball;

/// <summary>
/// A collectible melon — the capybara's favourite. Spins, bobs, glows, and pops
/// in a splash of green seeds when grabbed, with a satisfying blip.
/// </summary>
public partial class Melon : Area3D
{
    private Node3D _pivot;
    private MeshInstance3D _melon;
    private Light3D _light;
    private bool _taken;
    private float _t;

    public override void _Ready()
    {
        var col = new CollisionShape3D();
        col.Shape = new SphereShape3D { Radius = 0.7f };
        AddChild(col);

        _pivot = new Node3D();
        AddChild(_pivot);

        // Melon body — green rind with a little stripe.
        _melon = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.45f, Height = 0.9f, RadialSegments = 24 },
        };
        _melon.MaterialOverride = Assets.Material("melon");
        _pivot.AddChild(_melon);

        // Stem.
        var stem = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.08f, Height = 0.25f },
            Position = new Vector3(0, 0.45f, 0),
        };
        stem.MaterialOverride = Assets.Material("melon_stem");
        _pivot.AddChild(stem);

        _light = new OmniLight3D
        {
            LightColor = new Color(0.5f, 1f, 0.5f),
            LightEnergy = 1.4f,
            OmniRange = 4f,
        };
        _pivot.AddChild(_light);

        BodyEntered += OnBody;
    }

    public override void _Process(double delta)
    {
        if (_taken) return;
        _t += (float)delta;
        _pivot.RotateY((float)delta * 2.5f);
        _pivot.Position = new Vector3(0, Mathf.Sin(_t * 3f) * 0.18f, 0);
        ((OmniLight3D)_light).LightEnergy = 1.2f + Mathf.Sin(_t * 6f) * 0.4f;
    }

    private void OnBody(Node body)
    {
        if (_taken || body is not CapyballBall) return;
        _taken = true;
        Synth.Instance?.Pickup();
        Fx.Instance?.Burst(GlobalPosition, Vector3.Up, new Color(0.4f, 0.95f, 0.45f), 1.1f);
        var lvl = GetParentOrNull<LevelScene>();
        lvl?.OnMelonCollected(this);
        // Squash-pop then vanish.
        var tw = CreateTween();
        tw.TweenProperty(_pivot, "scale", new Vector3(1.5f, 1.5f, 1.5f), 0.08f);
        tw.TweenProperty(_pivot, "scale", Vector3.Zero, 0.12f);
        tw.TweenCallback(Callable.From(() => QueueFree()));
    }
}
