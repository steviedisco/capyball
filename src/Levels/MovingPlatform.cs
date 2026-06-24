using Godot;

namespace Capyball;

/// <summary>
/// A chunky platform that travels between two points on a sine. The body is
/// kinematic (AnimatableBody3D) so the ball rides it correctly via contact.
/// </summary>
public partial class MovingPlatform : AnimatableBody3D
{
    [Export] public Vector3 PointA = Vector3.Zero;
    [Export] public Vector3 PointB = new(0, 0, 6);
    [Export] public float Period = 4f;
    [Export] public Color Tint = Palette.Grape;
    [Export] public Vector3 Size = new(3f, 0.6f, 3f);

    private float _t;

    public override void _Ready()
    {
        var faceMat = Assets.MaterialTinted("platform", Tint);
        faceMat.EmissionEnergyMultiplier = 0.3f;
        var rimMat = Assets.MaterialTinted("platform_rim",
            new Color(Mathf.Min(1, Tint.R + 0.25f), Mathf.Min(1, Tint.G + 0.25f), Mathf.Min(1, Tint.B + 0.25f)));
        var mi = Procedural.PlatformBox(Size, faceMat, rimMat);
        AddChild(mi);
        // Synthesize a collision box matching visuals.
        var col = new CollisionShape3D();
        col.Shape = new BoxShape3D { Size = Size };
        AddChild(col);
    }

    public override void _PhysicsProcess(double delta)
    {
        _t += (float)delta;
        float s = (Mathf.Sin((_t / Period) * Mathf.Tau) + 1f) * 0.5f;
        GlobalPosition = PointA.Lerp(PointB, s);
    }
}
