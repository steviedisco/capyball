using Godot;

namespace Capyball;

/// <summary>
/// Level 2 — "Rise and Roll". Introduces a big launch ramp you tilt up, a
/// moving platform bridge, and a couple of bumpers to ricochet off. The path
/// bends, so camera-relative tilt steering gets a workout. Wider and longer.
/// </summary>
public partial class Level2 : LevelDefinition
{
    public const string LevelId = "lv2_riseandroll";

    public override LevelSpec Build()
    {
        var s = new LevelSpec
        {
            Id = LevelId,
            Title = "Rise and Roll",
            Subtitle = "tilt up the ramp, ride the bridge",
            SkyTop = new Color(0.42f, 0.30f, 0.82f),
            SkyBottom = new Color(0.92f, 0.55f, 0.85f),
            Start = new Vector3(0, 1, -30),
            Goal = new Vector3(0, 0, 36),
            NextId = Level3.LevelId,
        };

        Color grape = Palette.Grape, pink = Palette.HotPink, sunny = Palette.Sunny, ocean = Palette.Ocean, coral = Palette.Coral;

        // Start pad + lower runway.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0.5f, -30), Size = new Vector3(10, 2, 8), Color = grape, Emission = 0.3f });
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, -20), Size = new Vector3(10, 1, 12), Color = grape, Emission = 0.22f });

        // Big launch ramp — tilt forward to build speed, ride up.
        s.Ramps.Add(new LevelSpec.RampSpec { Pos = new Vector3(0, 1.0f, -8), Size = new Vector3(10, 0.6f, 10), Color = sunny, PitchDeg = -22, YawDeg = 0 });

        // Landing field after the ramp.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 6), Size = new Vector3(12, 1, 14), Color = pink, Emission = 0.28f });

        // A pair of bumpers in the field for pinball fun.
        s.Bumpers.Add(new LevelSpec.BumperSpec { Pos = new Vector3(-3.5f, 0.8f, 6), Tint = Palette.HotPink });
        s.Bumpers.Add(new LevelSpec.BumperSpec { Pos = new Vector3(3.5f, 0.8f, 6), Tint = Palette.HotPink });

        // Moving platform bridge over a dip.
        s.Movers.Add(new LevelSpec.MovingSpec
        {
            A = new Vector3(-5, 0, 18), B = new Vector3(5, 0, 18),
            Period = 3.5f, Tint = Palette.Coral, Size = new Vector3(4, 0.5f, 4),
        });

        // Approach + goal pad.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 26), Size = new Vector3(10, 1, 10), Color = ocean, Emission = 0.32f });
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0.5f, 36), Size = new Vector3(10, 2, 8), Color = sunny, Emission = 0.4f });

        // Perimeter walls.
        s.BuildWalls(half: new Vector2(11, 38), height: 2.5f, thickness: 1.0f, color: Palette.HotPink);

        // Melons — placed to tempt risk near the ramp and bridge.
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-3, 1.5f, -16) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 3.0f, -2) });   // off the ramp arc
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 1.5f, 18) });   // on the bridge
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-4, 1.5f, 26) });

        return s;
    }
}
