using Godot;

namespace Capyball;

/// <summary>
/// Level 1 — "First Steps". A wide, friendly runway with a gentle bend, a couple
/// of melons to chase, and a clear sightline to the goal. Teaches rolling + jump.
/// </summary>
public partial class Level1 : LevelDefinition
{
    public const string LevelId = "lv1_firststeps";

    public override LevelSpec Build()
    {
        var s = new LevelSpec
        {
            Id = LevelId,
            Title = "First Steps",
            Subtitle = "roll to the glowing goal",
            SkyTop = new Color(0.30f, 0.70f, 0.98f),
            SkyBottom = new Color(0.72f, 0.92f, 1.00f),
            Start = new Vector3(0, 1, -10),
            Goal = new Vector3(0, 0, 26),
            NextId = Level2.LevelId,
        };

        Color mint = Palette.Mint, sunny = Palette.Sunny, tangerine = Palette.Tangerine;

        // Starting pad — wide and forgiving.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, -10), Size = new Vector3(8, 1, 8), Color = mint, Emission = 0.25f });
        // Runway.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 4), Size = new Vector3(6, 1, 16), Color = mint, Emission = 0.2f });
        // A gentle bend (two offset chunks).
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(3, 0, 16), Size = new Vector3(8, 1, 8), Color = sunny, Emission = 0.3f });
        // Approach to goal.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 24), Size = new Vector3(7, 1, 8), Color = tangerine, Emission = 0.35f });

        // A small decorative rim of low blocks to frame the path.
        for (int i = 0; i < 5; i++)
        {
            float z = -6 + i * 8f;
            s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(-4.5f, 0.6f, z), Size = new Vector3(0.6f, 1.0f, 0.6f), Color = Palette.HotPink, Emission = 0.6f });
            s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(4.5f, 0.6f, z), Size = new Vector3(0.6f, 1.0f, 0.6f), Color = Palette.HotPink, Emission = 0.6f });
        }

        // Melons — an easy arc to encourage exploration.
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 1.5f, -2) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-2.5f, 1.5f, 8) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(3.5f, 1.5f, 16) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 1.5f, 22) });

        return s;
    }
}
