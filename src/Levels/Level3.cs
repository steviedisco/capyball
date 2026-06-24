using Godot;

namespace Capyball;

/// <summary>
/// Level 3 — "Bank and Bump". The showpiece. A long course that bends left then
/// right, a bumper gauntlet to ricochet through, a couple of boost pads to chain,
/// and a finale gate on a raised pedestal. Pure tilt — momentum and timing rule.
/// </summary>
public partial class Level3 : LevelDefinition
{
    public const string LevelId = "lv3_bankandbump";

    public override LevelSpec Build()
    {
        var s = new LevelSpec
        {
            Id = LevelId,
            Title = "Bank and Bump",
            Subtitle = "ricochet, chain the boosts, hit the gate",
            SkyTop = new Color(0.90f, 0.25f, 0.45f),
            SkyBottom = new Color(1.00f, 0.75f, 0.30f),
            Start = new Vector3(-22, 1, -34),
            Goal = new Vector3(0, 0, 42),
            NextId = "", // last level
        };

        Color pink = Palette.HotPink, sunny = Palette.Sunny, ocean = Palette.Ocean, coral = Palette.Coral, grape = Palette.Grape, tangerine = Palette.Tangerine;

        // Start pad (left side) + opening straight.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(-22, 0.5f, -34), Size = new Vector3(10, 2, 8), Color = pink, Emission = 0.35f });
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(-22, 0, -24), Size = new Vector3(10, 1, 14), Color = pink, Emission = 0.25f });

        // Bend right: a connecting field.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, -14), Size = new Vector3(40, 1, 16), Color = sunny, Emission = 0.22f });

        // Bumper gauntlet across the bend.
        s.Bumpers.Add(new LevelSpec.BumperSpec { Pos = new Vector3(-12, 0.8f, -14), Tint = Palette.Sunny });
        s.Bumpers.Add(new LevelSpec.BumperSpec { Pos = new Vector3(-4, 0.8f, -10), Tint = Palette.HotPink });
        s.Bumpers.Add(new LevelSpec.BumperSpec { Pos = new Vector3(6, 0.8f, -18), Tint = Palette.Sunny });
        s.Bumpers.Add(new LevelSpec.BumperSpec { Pos = new Vector3(12, 0.8f, -12), Tint = Palette.HotPink });

        // A boost pad to sling you down the course.
        s.Boosts.Add(new LevelSpec.BoostSpec { Pos = new Vector3(14, 0.3f, -14), Dir = Vector3.Forward, Force = 18, Tint = Palette.BoostFlame });

        // Long runway to the finale.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 10), Size = new Vector3(16, 1, 30), Color = coral, Emission = 0.25f });

        // A moving platform hop midway down the runway.
        s.Movers.Add(new LevelSpec.MovingSpec
        {
            A = new Vector3(-5, 0, 18), B = new Vector3(5, 0, 18),
            Period = 3.0f, Tint = Palette.Grape, Size = new Vector3(4, 0.5f, 4),
        });

        // Second boost to carry into the pedestal.
        s.Boosts.Add(new LevelSpec.BoostSpec { Pos = new Vector3(0, 0.3f, 30), Dir = Vector3.Forward, Force = 14, Tint = Palette.Grape });

        // Goal pedestal.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 1.0f, 42), Size = new Vector3(10, 3, 8), Color = tangerine, Emission = 0.5f });

        // Decorative pillars flanking the gate.
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.Pi / 2f + Mathf.Pi / 4f;
            s.Chunks.Add(new LevelSpec.Chunk
            {
                Pos = new Vector3(Mathf.Cos(a) * 7, 3, 42 + Mathf.Sin(a) * 4),
                Size = new Vector3(0.8f, 6, 0.8f),
                Color = Palette.Sunny,
                Emission = 0.7f,
            });
        }

        // Perimeter walls — biggest level.
        s.BuildWalls(half: new Vector2(30, 46), height: 3.0f, thickness: 1.2f, color: Palette.Ocean);

        // Bonus melons sprinkled through the gauntlet and runway.
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-22, 1.5f, -20) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 1.5f, -6) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-8, 1.5f, -14) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(8, 1.5f, -16) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 1.5f, 18) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-5, 1.5f, 30) });

        return s;
    }
}
