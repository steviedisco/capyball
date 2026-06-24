using Godot;

namespace Capyball;

/// <summary>
/// Helpers for assembling chunky meshes from inline primitives + disk-based
/// materials. Geometry that is a single primitive (sphere/box/torus/cylinder)
/// stays inline — it's one line and not worth a .glb. The *materials* (the part
/// worth editing) live as .tres files in <c>assets/materials/</c>.
///
/// The capybara model and ball shell are now authored assets loaded from
/// <c>assets/meshes/</c>; see <see cref="Assets"/>.
/// </summary>
public static class Procedural
{
    /// <summary>Chunky box platform with a slightly larger emissive glow rim.
    /// Materials are passed in (load via <see cref="Assets"/>).</summary>
    public static MeshInstance3D PlatformBox(Vector3 size, Material faceMat, Material rimMat)
    {
        var box = new BoxMesh { Size = size };
        box.Material = faceMat;
        var mi = new MeshInstance3D { Mesh = box };
        var rim = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size * 1.04f },
            MaterialOverride = rimMat,
        };
        rim.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        mi.AddChild(rim);
        return mi;
    }

    /// <summary>A spinning glow ring — used on goals, boost pads, bumpers.</summary>
    public static MeshInstance3D GlowRing(float radius, Material ringMat, float thickness = 0.18f)
    {
        var torus = new TorusMesh
        {
            InnerRadius = radius - thickness,
            OuterRadius = radius,
        };
        torus.Material = ringMat;
        return new MeshInstance3D { Mesh = torus };
    }

    /// <summary>Ball shell mesh — a translucent sphere using the disk ball_shell material.</summary>
    public static MeshInstance3D BallShell(float radius, Material shellMat)
    {
        var sphere = new SphereMesh { Radius = radius, Height = radius * 2, RadialSegments = 48, Rings = 24 };
        sphere.Material = shellMat;
        return new MeshInstance3D { Mesh = sphere };
    }
}
