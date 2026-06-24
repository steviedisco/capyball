using Godot;

namespace Capyball;

/// <summary>
/// Level 3 — "Pinball Palace". The showpiece: bumpers to ricochet between,
/// a zig-zag of narrow platforms, two boost jumps over void, and a finale
/// goal on a small pedestal. Demands boost timing and air control.
/// </summary>
public partial class Level3 : LevelDefinition
{
    public const string LevelId = "lv3_pinballpalace";

    public override LevelSpec Build()
    {
        var s = new LevelSpec
        {
            Id = LevelId,
            Title = "Pinball Palace",
            Subtitle = "bump, boost & thread the needle",
            SkyTop = new Color(0.90f, 0.25f, 0.45f),
            SkyBottom = new Color(1.00f, 0.75f, 0.30f),
            Start = new Vector3(0, 1, -14),
            Goal = new Vector3(0, 1, 40),
            NextId = "", // last level
        };

        Color pink = Palette.HotPink, sunny = Palette.Sunny, ocean = Palette.Ocean, coral = Palette.Coral, grape = Palette.Grape, tangerine = Palette.Tangerine;

        // Start pad.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, -14), Size = new Vector3(6, 1, 6), Color = pink, Emission = 0.35f });

        // Zig-zag narrow platforms.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, -7), Size = new Vector3(3, 1, 3), Color = sunny, Emission = 0.3f });
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(3, 0, -2), Size = new Vector3(3, 1, 3), Color = coral, Emission = 0.3f });
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(-3, 0, 3), Size = new Vector3(3, 1, 3), Color = sunny, Emission = 0.3f });
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(3, 0, 8), Size = new Vector3(3, 1, 3), Color = coral, Emission = 0.3f });

        // Bumper arena — a wider pad flanked by bumpers.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 14), Size = new Vector3(9, 1, 7), Color = grape, Emission = 0.3f });
        s.Bumpers.Add(new LevelSpec.BumperSpec { Pos = new Vector3(-2.5f, 0.8f, 13), Tint = Palette.HotPink });
        s.Bumpers.Add(new LevelSpec.BumperSpec { Pos = new Vector3(2.5f, 0.8f, 15), Tint = Palette.HotPink });
        s.Bumpers.Add(new LevelSpec.BumperSpec { Pos = new Vector3(0, 0.8f, 11), Tint = Palette.Sunny });

        // Boost launch over a big gap.
        s.Boosts.Add(new LevelSpec.BoostSpec { Pos = new Vector3(0, 0.3f, 17), Dir = Vector3.Forward, Force = 20, Tint = Palette.BoostFlame });

        // Mid-air melon to reward the boost.
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 3.5f, 22) });

        // Landing platform (narrow).
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 26), Size = new Vector3(4, 1, 4), Color = ocean, Emission = 0.35f });

        // Final boost to the pedestal.
        s.Boosts.Add(new LevelSpec.BoostSpec { Pos = new Vector3(0, 0.3f, 26), Dir = Vector3.Forward, Force = 16, Tint = Palette.Grape });

        // Goal pedestal.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 1, 40), Size = new Vector3(5, 1, 5), Color = tangerine, Emission = 0.5f });

        // Decorative pillars around the pedestal for drama.
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.Pi / 2f + Mathf.Pi / 4f;
            s.Chunks.Add(new LevelSpec.Chunk
            {
                Pos = new Vector3(Mathf.Cos(a) * 4, 2, 40 + Mathf.Sin(a) * 4),
                Size = new Vector3(0.6f, 4, 0.6f),
                Color = Palette.Sunny,
                Emission = 0.7f,
            });
        }

        // Bonus melons — placed perilously.
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(3, 1.5f, -2) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-3, 1.5f, 3) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-2.5f, 2.5f, 13) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(2.5f, 2.5f, 15) });

        return s;
    }
}
