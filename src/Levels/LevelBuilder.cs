using System.Collections.Generic;
using Godot;

namespace Capyball;

/// <summary>
/// Helpers that turn high-level level intent (a rectangular play area) into the
/// concrete wall chunks that fence it in, so each level only declares its size.
/// </summary>
public static class LevelBuilder
{
    /// <summary>Generates four perimeter walls around a rectangular play area,
    /// leaving them in <paramref name="spec"/>'s Walls list. The play area is
    /// centred on the origin, extending ±<paramref name="half"/> in X and Z.</summary>
    public static void BuildWalls(this LevelSpec spec, Vector2 half, float height, float thickness, Color color)
    {
        spec.PlayArea = half;
        float x = half.X;
        float z = half.Y;
        // Four rails: -X, +X, -Z, +Z edges.
        spec.Walls.Add(new LevelSpec.WallSpec
        {
            Pos = new Vector3(-x - thickness * 0.5f, height * 0.5f, 0),
            Size = new Vector3(thickness, height, z * 2f + thickness * 2f),
            Color = color,
        });
        spec.Walls.Add(new LevelSpec.WallSpec
        {
            Pos = new Vector3(x + thickness * 0.5f, height * 0.5f, 0),
            Size = new Vector3(thickness, height, z * 2f + thickness * 2f),
            Color = color,
        });
        spec.Walls.Add(new LevelSpec.WallSpec
        {
            Pos = new Vector3(0, height * 0.5f, -z - thickness * 0.5f),
            Size = new Vector3(x * 2f + thickness * 2f, height, thickness),
            Color = color,
        });
        spec.Walls.Add(new LevelSpec.WallSpec
        {
            Pos = new Vector3(0, height * 0.5f, z + thickness * 0.5f),
            Size = new Vector3(x * 2f + thickness * 2f, height, thickness),
            Color = color,
        });
    }
}
