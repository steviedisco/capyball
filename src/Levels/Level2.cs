using Godot;

namespace Capyball;

/// <summary>
/// Level 2 — "Ups and Downs". Introduces ramps, a boost pad jump, and a moving
/// platform ride. Tighter path with melons placed to tempt risk.
/// </summary>
public partial class Level2 : LevelDefinition
{
    public const string LevelId = "lv2_upsanddowns";

    public override LevelSpec Build()
    {
        var s = new LevelSpec
        {
            Id = LevelId,
            Title = "Ups and Downs",
            Subtitle = "ramps, boosts & a moving bridge",
            SkyTop = new Color(0.42f, 0.30f, 0.82f),
            SkyBottom = new Color(0.92f, 0.55f, 0.85f),
            Start = new Vector3(0, 1, -12),
            Goal = new Vector3(0, 0, 34),
            NextId = Level3.LevelId,
        };

        Color grape = Palette.Grape, pink = Palette.HotPink, sunny = Palette.Sunny, ocean = Palette.Ocean;

        // Start pad.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, -12), Size = new Vector3(7, 1, 7), Color = grape, Emission = 0.3f });

        // Runway up to a ramp.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, -4), Size = new Vector3(5, 1, 8), Color = grape, Emission = 0.25f });

        // Ramp #1 — launches you forward/up.
        s.Ramps.Add(new LevelSpec.RampSpec { Pos = new Vector3(0, 0.5f, 2), Size = new Vector3(5, 0.4f, 4), Color = sunny, PitchDeg = -16, YawDeg = 0 });

        // Landing pad after the ramp.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 10), Size = new Vector3(5, 1, 5), Color = pink, Emission = 0.35f });

        // A boost pad to carry across a gap.
        s.Boosts.Add(new LevelSpec.BoostSpec { Pos = new Vector3(0, 0.3f, 10), Dir = Vector3.Forward, Force = 18, Tint = Palette.BoostFlame });

        // The gap (no platform) — boosts you over it. Landing platform.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 18), Size = new Vector3(5, 1, 4), Color = ocean, Emission = 0.3f });

        // Moving platform bridge over the void.
        s.Movers.Add(new LevelSpec.MovingSpec
        {
            A = new Vector3(-4, 0, 24), B = new Vector3(4, 0, 24),
            Period = 3.5f, Tint = Palette.Coral, Size = new Vector3(3, 0.5f, 3),
        });

        // Final approach + goal pad.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 30), Size = new Vector3(6, 1, 6), Color = sunny, Emission = 0.4f });

        // Melons — placed over the gap & on the moving bridge to tempt the player.
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 2.5f, 6) });     // off the ramp arc
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 1.5f, 14) });    // mid-gap (risky)
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 1.5f, 24) });    // on the moving bridge
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-3, 1.5f, 30) });

        return s;
    }
}
