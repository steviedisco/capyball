using Godot;

namespace Capyball;

/// <summary>
/// Helpers for building chunky, candy-coloured meshes and materials at runtime.
/// No external assets — everything is generated so the repo stays asset-light.
/// </summary>
public static class Procedural
{
    /// <summary>A glossy, slightly emissive StandardMaterial in a given colour.</summary>
    public static StandardMaterial3D Candy(Color color, float emission = 0.25f, float rough = 0.35f)
    {
        var m = new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = rough,
            Metallic = 0.0f,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = emission,
        };
        return m;
    }

    /// <summary>Strong glow material, unshaded — for beams, rings, energy.</summary>
    public static StandardMaterial3D Glow(Color color, float energy = 3f)
    {
        var m = new StandardMaterial3D
        {
            AlbedoColor = color,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = energy,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Roughness = 0.0f,
        };
        return m;
    }

    /// <summary>Chunky box platform with rounded visual edges via a slightly larger emissive under-shell.</summary>
    public static MeshInstance3D PlatformBox(Vector3 size, Color color, float emission = 0.25f)
    {
        var box = new BoxMesh { Size = size };
        box.Material = Candy(color, emission);
        var mi = new MeshInstance3D { Mesh = box };
        // A subtly larger, brighter "rim" box behind it for that Sega glow outline.
        var rim = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size * 1.04f },
            MaterialOverride = Glow(new Color(Mathf.Min(1, color.R + 0.25f), Mathf.Min(1, color.G + 0.25f), Mathf.Min(1, color.B + 0.25f)), 1.6f),
        };
        // Don't cull the rim glow.
        rim.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        mi.AddChild(rim);
        return mi;
    }

    /// <summary>A spinning glow ring — used on goals, boost pads, checkpoints.</summary>
    public static MeshInstance3D GlowRing(float radius, Color color, float thickness = 0.18f)
    {
        var torus = new TorusMesh
        {
            InnerRadius = radius - thickness,
            OuterRadius = radius,
        };
        torus.Material = Glow(color, 2.4f);
        return new MeshInstance3D { Mesh = torus };
    }

    /// <summary>Build the capybara body cluster (head + body + ears + snout) as a single skinned-ish mesh group.</summary>
    public static Node3D CapybaraModel()
    {
        var root = new Node3D { Name = "CapyModel" };

        var furMat = Candy(Palette.CapyFur, emission: 0.05f, rough: 0.7f);
        var furLightMat = Candy(Palette.CapyFurLight, emission: 0.05f, rough: 0.7f);
        var snoutMat = Candy(Palette.CapySnout, emission: 0.05f, rough: 0.6f);
        var noseMat = Candy(Palette.CapyNose, emission: 0.0f, rough: 0.5f);

        MeshInstance3D Make(PrimitiveMesh mesh, Material mat, string name, Vector3 pos, Vector3 scale)
        {
            mesh.Material = mat;
            var mi = new MeshInstance3D { Name = name, Mesh = mesh, Position = pos, Scale = scale };
            root.AddChild(mi);
            return mi;
        }

        // Body — chunky egg
        var body = Make(new SphereMesh { Radius = 0.55f, Height = 1.1f }, furMat, "Body",
            new Vector3(0, 0, 0), new Vector3(1.0f, 1.15f, 1.4f));
        // Head
        var head = Make(new SphereMesh { Radius = 0.42f, Height = 0.84f }, furMat, "Head",
            new Vector3(0, 0.28f, 0.55f), Vector3.One);
        // Snout
        Make(new SphereMesh { Radius = 0.24f, Height = 0.48f }, snoutMat, "Snout",
            new Vector3(0, 0.18f, 0.85f), new Vector3(1, 0.8f, 1.1f));
        // Nose
        Make(new SphereMesh { Radius = 0.09f, Height = 0.18f }, noseMat, "Nose",
            new Vector3(0, 0.24f, 1.03f), Vector3.One);
        // Ears
        Make(new SphereMesh { Radius = 0.12f, Height = 0.24f }, furLightMat, "EarL",
            new Vector3(-0.26f, 0.56f, 0.5f), Vector3.One);
        Make(new SphereMesh { Radius = 0.12f, Height = 0.24f }, furLightMat, "EarR",
            new Vector3(0.26f, 0.56f, 0.5f), Vector3.One);
        // Eyes — glossy dark beads with a white highlight
        var eyeWhite = Candy(new Color(1, 1, 1), emission: 0.4f, rough: 0.2f);
        Make(new SphereMesh { Radius = 0.085f, Height = 0.17f }, noseMat, "EyeL",
            new Vector3(-0.16f, 0.36f, 0.86f), Vector3.One).MaterialOverride = Candy(new Color(0.05f, 0.05f, 0.08f), 0.2f, 0.2f);
        Make(new SphereMesh { Radius = 0.085f, Height = 0.17f }, noseMat, "EyeR",
            new Vector3(0.16f, 0.36f, 0.86f), Vector3.One).MaterialOverride = Candy(new Color(0.05f, 0.05f, 0.08f), 0.0f, 0.2f);
        Make(new SphereMesh { Radius = 0.03f, Height = 0.06f }, eyeWhite, "EyeLHi",
            new Vector3(-0.13f, 0.39f, 0.92f), Vector3.One);
        Make(new SphereMesh { Radius = 0.03f, Height = 0.06f }, eyeWhite, "EyeRHi",
            new Vector3(0.19f, 0.39f, 0.92f), Vector3.One);

        root.RotateY(Mathf.DegToRad(-90)); // face +Z initially
        return root;
    }

    /// <summary>Squash-and-stretch friendly capsule body for the ball shell.</summary>
    public static MeshInstance3D BallShell(float radius)
    {
        var sphere = new SphereMesh { Radius = radius, Height = radius * 2, RadialSegments = 48, Rings = 24 };
        var mat = new StandardMaterial3D
        {
            AlbedoColor = Palette.BallTint,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 0.05f,
            Metallic = 0.0f,
            EmissionEnabled = true,
            Emission = Palette.BallRim,
            EmissionEnergyMultiplier = 0.8f,
            RefractionEnabled = true,
            RefractionScale = 0.04f,
        };
        sphere.Material = mat;
        return new MeshInstance3D { Mesh = sphere };
    }
}
