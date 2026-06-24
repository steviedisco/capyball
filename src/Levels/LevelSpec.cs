using System.Collections.Generic;
using Godot;

namespace Capyball;

/// <summary>
/// Declarative description of a level — a list of chunks (platforms) plus props.
/// Levels are built from this so they're easy to author and tweak by hand.
/// </summary>
public class LevelSpec
{
    public string Id;
    public string Title;
    public string Subtitle;
    public Color SkyTop;
    public Color SkyBottom;
    public Vector3 Start = Vector3.Zero;
    public Vector3 Goal = new(0, 0, 20);
    public string NextId = "";

    public readonly List<Chunk> Chunks = new();
    public readonly List<RampSpec> Ramps = new();
    public readonly List<MelonSpec> Melons = new();
    public readonly List<BoostSpec> Boosts = new();
    public readonly List<MovingSpec> Movers = new();
    public readonly List<BumperSpec> Bumpers = new();

    public struct Chunk { public Vector3 Pos; public Vector3 Size; public Color Color; public float Emission; }
    public struct RampSpec { public Vector3 Pos; public Vector3 Size; public Color Color; public float PitchDeg; public float YawDeg; }
    public struct MelonSpec { public Vector3 Pos; }
    public struct BoostSpec { public Vector3 Pos; public Vector3 Dir; public float Force; public Color Tint; }
    public struct MovingSpec { public Vector3 A; public Vector3 B; public float Period; public Color Tint; public Vector3 Size; }
    public struct BumperSpec { public Vector3 Pos; public Color Tint; }
}

/// <summary>Base class for level definitions. Subclasses fill the spec in Build().
/// Each subclass also exposes a <c>public const string LevelId</c> so it can be
/// referenced statically for cross-level wiring and the registry.</summary>
public abstract partial class LevelDefinition : Resource
{
    public abstract LevelSpec Build();
}

/// <summary>Registry of all levels, in order of progression.</summary>
public static class Levels
{
    public static readonly string[] Order = { Level1.LevelId, Level2.LevelId, Level3.LevelId };

    public static LevelSpec Get(string id) => id switch
    {
        Level1.LevelId => new Level1().Build(),
        Level2.LevelId => new Level2().Build(),
        Level3.LevelId => new Level3().Build(),
        _ => new Level1().Build(),
    };

    public static string NextOf(string id)
    {
        int i = System.Array.IndexOf(Order, id);
        if (i < 0 || i + 1 >= Order.Length) return null;
        return Order[i + 1];
    }
}
