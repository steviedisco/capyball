using Godot;

namespace Capyball;

/// <summary>
/// Helpers for assembling chunky meshes from inline primitives + disk-based
/// materials + textures. Geometry that is a single primitive stays inline;
/// the *materials* and *textures* (the part worth editing) live on disk.
/// </summary>
public static class Procedural
{
    /// <summary>Chunky box platform with a textured face, a glowing rim outline,
    /// and a beveled inset panel on top for depth (so it's not a flat slab).</summary>
    public static MeshInstance3D PlatformBox(Vector3 size, Material faceMat, Material rimMat)
    {
        var box = new BoxMesh { Size = size };
        box.Material = faceMat;
        var mi = new MeshInstance3D { Mesh = box };

        // Tinted emissive glow rim — slightly larger, unshaded, no shadow.
        var rim = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size * 1.04f },
            MaterialOverride = rimMat,
        };
        rim.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        mi.AddChild(rim);

        // Beveled inset panel on the top surface — a slightly smaller, brighter
        // box sitting just above the top face. Gives platforms visible depth/edges.
        float top = size.Y * 0.5f;
        var insetSize = new Vector3(size.X * 0.9f, 0.12f, size.Z * 0.9f);
        var inset = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = insetSize },
            Position = new Vector3(0, top + 0.06f, 0),
            MaterialOverride = rimMat,
        };
        inset.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        mi.AddChild(inset);

        return mi;
    }

    /// <summary>A wall segment: a tall box plus a bright cap rail along the top
    /// edge so it reads as a guardrail, not a flat slab.</summary>
    public static MeshInstance3D WallSegment(Vector3 size, Material bodyMat, Material railMat)
    {
        var box = new BoxMesh { Size = size };
        box.Material = bodyMat;
        var mi = new MeshInstance3D { Mesh = box };

        // Bright cap rail across the top.
        float top = size.Y * 0.5f;
        var rail = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(size.X * 1.02f, 0.25f, size.Z * 1.02f) },
            Position = new Vector3(0, top + 0.12f, 0),
            MaterialOverride = railMat,
        };
        rail.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        mi.AddChild(rail);
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

    /// <summary>Set how many times a material's texture tiles across a surface, so
    /// checkers read at a sensible world size instead of stretching once per face.</summary>
    public static void SetUvScale(Material mat, Vector3 scale)
    {
        if (mat is StandardMaterial3D sm)
        {
            sm.Uv1Scale = scale;
            sm.Uv1Triplanar = true; // project the texture in world space so box faces align
        }
    }
}
