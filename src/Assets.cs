using System.Collections.Generic;
using Godot;

namespace Capyball;

/// <summary>
/// Loads disk-based resources (.tres materials, the capybara .tscn) from
/// <c>res://assets/</c> and caches them. For materials that need to vary per
/// instance or be mutated at runtime, callers ask for a <see cref="MaterialTinted"/>
/// (recoloured copy) or <see cref="MaterialMutable"/> (independent copy).
///
/// Read-only shared materials (e.g. capybara parts, the goal beam) can be fetched
/// straight from <see cref="Material"/> — one instance is reused safely.
/// </summary>
public static class Assets
{
    private const string MatDir = "res://assets/materials/";
    private const string MeshDir = "res://assets/meshes/";

    private static readonly Dictionary<string, Resource> _cache = new();

    private static T Load<T>(string path) where T : Resource
    {
        if (_cache.TryGetValue(path, out var cached) && cached is T typed)
            return typed;
        var res = ResourceLoader.Load<T>(path);
        if (res != null) _cache[path] = res;
        return res;
    }

    /// <summary>Loads a shared, read-only <see cref="StandardMaterial3D"/> by name
    /// (without extension). Safe to share across many instances.</summary>
    public static StandardMaterial3D Material(string name)
        => Load<StandardMaterial3D>(MatDir + name + ".tres");

    /// <summary>Loads a <see cref="ParticleProcessMaterial"/> by name (without extension).</summary>
    public static ParticleProcessMaterial ParticleMaterial(string name)
        => Load<ParticleProcessMaterial>(MatDir + name + ".tres");

    /// <summary>Returns an independent copy of a material (a fresh
    /// <see cref="StandardMaterial3D"/> whose fields can be mutated per-frame
    /// without affecting other instances). Used by the trail, boost flash, etc.</summary>
    public static StandardMaterial3D MaterialMutable(string name)
        => (StandardMaterial3D)Material(name).Duplicate();

    /// <summary>Returns a recoloured copy of a material — <c>AlbedoColor</c> and
    /// <c>Emission</c> set to <paramref name="color"/>. Each call is an independent
    /// instance, so per-platform/per-prop tinting never bleeds across instances.</summary>
    public static StandardMaterial3D MaterialTinted(string name, Color color)
    {
        var m = (StandardMaterial3D)Material(name).Duplicate();
        m.AlbedoColor = color;
        if (m.EmissionEnabled) m.Emission = color;
        return m;
    }

    /// <summary>The exported capybara model scene.</summary>
    public static PackedScene Capybara()
        => Load<PackedScene>(MeshDir + "capybara.tscn");
}
