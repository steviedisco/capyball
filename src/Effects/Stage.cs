using Godot;

namespace Capyball;

/// <summary>
/// Builds the juicy, Sega-bright environment for a stage: gradient sky,
/// warm key light + cool fill, ambient probes, a contact shadow sun, and the
/// post-processing stack (glow/bloom, saturation, vignette, screen-space ref-ish).
/// One call per level keeps every stage consistent.
/// </summary>
public static class Stage
{
    public static WorldEnvironment BuildEnvironment(Node addTo, Color skyTop, Color skyBottom)
    {
        var we = new WorldEnvironment();
        var env = new Environment();

        // Background — vertical gradient via sky.
        var sky = new ProceduralSkyMaterial
        {
            SkyTopColor = skyTop,
            SkyHorizonColor = skyBottom,
            SunAngleMax = 35f,
            SunCurve = 0.3f,
            GroundBottomColor = new Color(skyBottom.R * 0.6f, skyBottom.G * 0.6f, skyBottom.B * 0.6f),
            GroundHorizonColor = skyBottom,
        };
        env.Sky = new Sky { SkyMaterial = sky };
        env.BackgroundMode = Environment.BGMode.Sky;

        // Ambient + main lights.
        env.AmbientLightSource = Environment.AmbientSource.Sky;
        env.AmbientLightColor = new Color(1.0f, 0.97f, 0.88f);
        env.AmbientLightEnergy = 0.55f;

        // Fog for depth pop — subtle.
        env.FogEnabled = true;
        env.FogLightColor = skyBottom;
        env.FogLightEnergy = 0.6f;
        env.FogDensity = 0.012f;
        env.FogAerialPerspective = 0.4f;

        // The juice — post processing.
        env.GlowEnabled = true;
        env.GlowIntensity = 1.15f;
        env.GlowStrength = 1.1f;
        env.GlowMix = 0.18f;
        env.GlowMapStrength = 0.8f;
        // Enable the mid-brightness glow levels (indices 2–5) for the Sega pop.
        for (int i = 2; i <= 5; i++) env.SetGlowLevel(i, 1.0f);
        env.GlowNormalized = false;

        env.TonemapMode = Environment.ToneMapper.Filmic;
        env.TonemapExposure = 1.05f;
        env.TonemapWhite = 1.15f;

        env.SsrEnabled = false;
        env.SsaoEnabled = true;
        env.SsaoIntensity = 1.6f;
        env.SsaoRadius = 1.2f;
        env.SsaoPower = 1.6f;

        env.AdjustmentEnabled = true;
        env.AdjustmentBrightness = 1.02f;
        env.AdjustmentContrast = 1.08f;
        env.AdjustmentSaturation = 1.28f;

        we.Environment = env;
        addTo.AddChild(we);

        // Sun light.
        var sun = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-48, 30, 0),
            LightColor = new Color(1.0f, 0.95f, 0.82f),
            LightEnergy = 1.15f,
            ShadowEnabled = true,
            DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel2Splits,
            DirectionalShadowBlendSplits = true,
        };
        sun.DirectionalShadowMaxDistance = 80f;
        addTo.AddChild(sun);

        return we;
    }

    /// <summary>A glowing translucent "fall plane" far below — visually communicates the void.</summary>
    public static MeshInstance3D BuildVoidPlane(Node addTo, float y = -25f, float size = 400f)
    {
        var plane = new PlaneMesh { Size = new Vector2(size, size) };
        plane.Material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.04f, 0.06f, 0.18f),
            EmissionEnabled = true,
            Emission = new Color(0.10f, 0.16f, 0.40f),
            EmissionEnergyMultiplier = 0.6f,
            Roughness = 1f,
        };
        var mi = new MeshInstance3D { Mesh = plane, Position = new Vector3(0, y, 0) };
        addTo.AddChild(mi);
        return mi;
    }

    /// <summary>Adds a slowly tumbling field of distant decorative shapes for depth sparkle.</summary>
    public static void AddDistantSparkles(Node addTo, int count, float radius, float height)
    {
        var mat = Procedural.Glow(new Color(1f, 0.95f, 0.7f), 2.5f);
        for (int i = 0; i < count; i++)
        {
            float a = (float)GD.RandRange(0, Mathf.Tau);
            float r = (float)GD.RandRange(radius * 0.4f, radius);
            var mi = new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.4f, Height = 0.8f },
                Position = new Vector3(Mathf.Cos(a) * r, (float)GD.RandRange(height * 0.3f, height), Mathf.Sin(a) * r),
                MaterialOverride = mat,
            };
            addTo.AddChild(mi);
            var tw = mi.CreateTween().SetLoops();
            tw.TweenProperty(mi, "position:y", mi.Position.Y - 1.5f, GD.RandRange(3f, 6f)).SetTrans(Tween.TransitionType.Sine);
            tw.TweenProperty(mi, "position:y", mi.Position.Y + 1.5f, GD.RandRange(3f, 6f)).SetTrans(Tween.TransitionType.Sine);
        }
    }
}
