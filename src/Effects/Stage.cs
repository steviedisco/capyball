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

        // Background — a richer 3-stop gradient sky for more atmosphere.
        var sky = new ProceduralSkyMaterial
        {
            SkyTopColor = skyTop,
            SkyHorizonColor = skyBottom,
            SunAngleMax = 28f,
            SunCurve = 0.25f,
            GroundBottomColor = new Color(skyBottom.R * 0.45f, skyBottom.G * 0.45f, skyBottom.B * 0.45f),
            GroundHorizonColor = skyBottom,
            SkyEnergyMultiplier = 1.05f,
            GroundEnergyMultiplier = 0.5f,
        };
        env.Sky = new Sky { SkyMaterial = sky };
        env.BackgroundMode = Environment.BGMode.Sky;

        // Warm ambient to lift the candy palette.
        env.AmbientLightSource = Environment.AmbientSource.Sky;
        env.AmbientLightColor = new Color(1.0f, 0.96f, 0.85f);
        env.AmbientLightEnergy = 0.7f;
        env.AmbientLightSkyContribution = 1.0f;

        // Fog for depth — stronger so distant course fades into the haze.
        env.FogEnabled = true;
        env.FogLightColor = skyBottom;
        env.FogLightEnergy = 0.9f;
        env.FogDensity = 0.02f;
        env.FogAerialPerspective = 0.55f;
        env.FogSunScatter = 0.35f;

        // The juice — punchier glow/bloom for the Sega pop.
        env.GlowEnabled = true;
        env.GlowIntensity = 1.4f;
        env.GlowStrength = 1.2f;
        env.GlowMix = 0.22f;
        env.GlowMapStrength = 0.8f;
        env.GlowBloom = 0.25f;
        for (int i = 2; i <= 5; i++) env.SetGlowLevel(i, 1.0f);
        env.GlowNormalized = false;

        env.TonemapMode = Environment.ToneMapper.Filmic;
        env.TonemapExposure = 1.08f;
        env.TonemapWhite = 1.2f;

        env.SsrEnabled = false;
        env.SsaoEnabled = true;
        env.SsaoIntensity = 2.0f;
        env.SsaoRadius = 1.0f;
        env.SsaoPower = 1.8f;
        env.SsaoHorizon = 0.4f;
        env.SsaoSharpness = 1.0f;

        // Saturation + contrast punch for the arcade look.
        env.AdjustmentEnabled = true;
        env.AdjustmentBrightness = 1.03f;
        env.AdjustmentContrast = 1.12f;
        env.AdjustmentSaturation = 1.35f;

        we.Environment = env;
        addTo.AddChild(we);

        // Key light — warm sun from upper-left.
        var sun = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-52, -35, 0),
            LightColor = new Color(1.0f, 0.93f, 0.78f),
            LightEnergy = 1.35f,
            ShadowEnabled = true,
            DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel2Splits,
            DirectionalShadowBlendSplits = true,
        };
        sun.DirectionalShadowMaxDistance = 120f;
        addTo.AddChild(sun);

        // Fill light — cool, from the opposite side, to model form and kill flat shadows.
        var fill = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-40, 150, 0),
            LightColor = new Color(0.6f, 0.72f, 1.0f),
            LightEnergy = 0.45f,
            ShadowEnabled = false,
        };
        addTo.AddChild(fill);

        return we;
    }

    /// <summary>A glowing translucent "fall plane" far below — visually communicates the void.</summary>
    public static MeshInstance3D BuildVoidPlane(Node addTo, float y = -25f, float size = 400f)
    {
        var plane = new PlaneMesh { Size = new Vector2(size, size) };
        plane.Material = Assets.Material("void_plane");
        var mi = new MeshInstance3D { Mesh = plane, Position = new Vector3(0, y, 0) };
        addTo.AddChild(mi);
        return mi;
    }

    /// <summary>Adds a slowly tumbling field of distant decorative shapes for depth sparkle.</summary>
    public static void AddDistantSparkles(Node addTo, int count, float radius, float height)
    {
        // Shared read-only glow material loaded from disk.
        var mat = Assets.Material("sparkle");
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

    /// <summary>Soft drifting billboard clouds high above the course for atmosphere
    /// and a sense of place. Built from clusters of translucent unshaded spheres.</summary>
    public static void AddClouds(Node addTo, int count, float radius, float height)
    {
        var cloudMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 1f, 1f, 0.85f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            EmissionEnabled = true,
            Emission = new Color(1f, 0.98f, 0.95f),
            EmissionEnergyMultiplier = 0.4f,
            Roughness = 1f,
            NoDepthTest = true,
        };
        for (int i = 0; i < count; i++)
        {
            var cloud = new Node3D();
            float a = (float)GD.RandRange(0, Mathf.Tau);
            float r = (float)GD.RandRange(radius * 0.3f, radius);
            cloud.Position = new Vector3(Mathf.Cos(a) * r, height + (float)GD.RandRange(-4, 8), Mathf.Sin(a) * r);
            // Each cloud = a few overlapping puffs.
            int puffs = (int)GD.RandRange(3, 6);
            for (int p = 0; p < puffs; p++)
            {
                var puff = new MeshInstance3D
                {
                    Mesh = new SphereMesh { Radius = (float)GD.RandRange(2.5f, 4.5f), Height = (float)GD.RandRange(5f, 9f) },
                    Position = new Vector3((float)GD.RandRange(-3, 3), (float)GD.RandRange(-0.5f, 0.5f), (float)GD.RandRange(-3, 3)),
                    Scale = new Vector3(1f, 0.6f, 1f),
                    MaterialOverride = cloudMat,
                };
                puff.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                cloud.AddChild(puff);
            }
            addTo.AddChild(cloud);
            // Drift slowly across the sky.
            var tw = cloud.CreateTween().SetLoops();
            tw.TweenProperty(cloud, "position:x", cloud.Position.X + 20f, (float)GD.RandRange(20f, 40f)).SetTrans(Tween.TransitionType.Sine);
            tw.TweenProperty(cloud, "position:x", cloud.Position.X - 20f, (float)GD.RandRange(20f, 40f)).SetTrans(Tween.TransitionType.Sine);
        }
    }
}
