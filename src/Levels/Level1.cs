using Godot;

namespace Capyball;

/// <summary>
/// Level 1 — "First Roll". A wide, forgiving field that teaches the tilt
/// mechanic. The whole floor gently slopes toward the goal so a new player
/// rolling forward makes steady progress; perimeter walls keep them in.
/// Scale: ~2.5× the old size so there's room to build momentum.
/// </summary>
public partial class Level1 : LevelDefinition
{
    public const string LevelId = "lv1_firstroll";

    public override LevelSpec Build()
    {
        var s = new LevelSpec
        {
            Id = LevelId,
            Title = "First Roll",
            Subtitle = "tilt the world to roll — reach the gate",
            SkyTop = new Color(0.30f, 0.70f, 0.98f),
            SkyBottom = new Color(0.72f, 0.92f, 1.00f),
            Start = new Vector3(0, 1, -26),
            Goal = new Vector3(0, 0, 30),
            NextId = Level2.LevelId,
        };

        Color mint = Palette.Mint, sunny = Palette.Sunny, tangerine = Palette.Tangerine, ocean = Palette.Ocean;

        // Main field — one big wide floor running from start to goal.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0, 0), Size = new Vector3(16, 1, 60), Color = mint, Emission = 0.22f });

        // A raised start pad so the ball begins slightly above the field.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0.5f, -26), Size = new Vector3(10, 2, 8), Color = sunny, Emission = 0.3f });

        // A couple of gentle speed-bump ramps to show off the tilt feel.
        s.Ramps.Add(new LevelSpec.RampSpec { Pos = new Vector3(0, 0.4f, -10), Size = new Vector3(10, 0.8f, 3), Color = tangerine, PitchDeg = -10, YawDeg = 0 });
        s.Ramps.Add(new LevelSpec.RampSpec { Pos = new Vector3(0, 0.4f, 10), Size = new Vector3(10, 0.8f, 3), Color = tangerine, PitchDeg = -10, YawDeg = 0 });

        // Goal pad at the far end.
        s.Chunks.Add(new LevelSpec.Chunk { Pos = new Vector3(0, 0.5f, 30), Size = new Vector3(10, 2, 8), Color = ocean, Emission = 0.4f });

        // Perimeter walls — wide field.
        s.BuildWalls(half: new Vector2(9, 32), height: 2.5f, thickness: 1.0f, color: Palette.HotPink);

        // Melons — a gentle arc to chase along the way.
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-3, 1.5f, -14) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(3, 1.5f, -4) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(-3, 1.5f, 6) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(3, 1.5f, 18) });
        s.Melons.Add(new LevelSpec.MelonSpec { Pos = new Vector3(0, 1.5f, 24) });

        return s;
    }
}
