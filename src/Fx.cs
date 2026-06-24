using System.Collections.Generic;
using Godot;

namespace Capyball;

/// <summary>
/// One-stop-shop for juicy effects. Any node can call Fx.Burst(...) and a
/// pooled particle system will handle it. Keeps the per-level code clean.
/// </summary>
public partial class Fx : Node
{
    public static Fx Instance { get; private set; }

    // Camera hooks — set by the LevelScene so FX can punch the camera.
    public System.Action<float> ShakeCamera;
    public System.Action<float, float> TimeFreeze; // (duration, timescale)

    private readonly List<GpuParticles3D> _pool = new();
    private readonly Queue<(Vector3 pos, Vector3 normal, Color color, float scale)> _burstQueue = new();

    public override void _Ready()
    {
        Instance = this;
        ProcessPriority = -100; // run FX before gameplay so bursts land this frame
    }

    public override void _PhysicsProcess(double delta)
    {
        while (_burstQueue.Count > 0)
        {
            var b = _burstQueue.Dequeue();
            SpawnBurst(b.pos, b.normal, b.color, b.scale);
        }
    }

    /// <summary>Splashy directional burst of colour, great for landings, bumps, pickups.</summary>
    public void Burst(Vector3 pos, Vector3 normal, Color color, float scale = 1f)
        => _burstQueue.Enqueue((pos, normal, color, scale));

    /// <summary>Confetti shower for level completion.</summary>
    public void Confetti(Vector3 pos)
    {
        var colors = Palette.Confetti;
        for (int i = 0; i < 3; i++)
        {
            var p = MakeOneShot(60, colors[i % colors.Length]);
            AddChild(p);
            p.GlobalPosition = pos + new Vector3(GD.RandRange(-2, 2), 3, GD.RandRange(-2, 2));
            p.Emitting = true;
        }
    }

    /// <summary>Punch the screen. magnitude ~0.2 is gentle, ~0.8 is violent.</summary>
    public void Shake(float magnitude) => ShakeCamera?.Invoke(magnitude);

    /// <summary>Slow / freeze time briefly for impact emphasis.</summary>
    public void Freeze(float duration, float timescale = 0.2f) => TimeFreeze?.Invoke(duration, timescale);

    private void SpawnBurst(Vector3 pos, Vector3 normal, Color color, float scale)
    {
        var p = MakeOneShot(28, color);
        AddChild(p);
        p.GlobalPosition = pos;
        // Orient so the burst fires along the surface normal. LookingAt throws if
        // its target axis is parallel to the up vector — pick an up that's never
        // parallel to the normal (falls back when the normal is straight up/down).
        Vector3 dir = normal.LengthSquared() > 0.0001f ? normal.Normalized() : Vector3.Up;
        Vector3 up = Mathf.Abs(dir.Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up;
        p.GlobalTransform = p.GlobalTransform.LookingAt(pos + dir, up);
        p.Scale = Vector3.One * scale;
        p.Emitting = true;
    }

    private GpuParticles3D MakeOneShot(int amount, Color color)
    {
        var p = new GpuParticles3D
        {
            Amount = amount,
            Lifetime = 0.8,
            OneShot = true,
            Emitting = false,
            FixedFps = 60,
            VisibilityAabb = new Aabb(new Vector3(-8, -8, -8), new Vector3(16, 16, 16)),
        };

        // Particle process material loaded from disk, tinted to the event colour.
        var mat = (ParticleProcessMaterial)Assets.ParticleMaterial("burst_particle").Duplicate();
        mat.Color = color;
        p.ProcessMaterial = mat;

        var quad = new QuadMesh { Size = new Vector2(0.22f, 0.22f) };
        var qmat = Assets.MaterialTinted("burst", color);
        qmat.Emission = color * 1.6f;
        quad.Material = qmat;
        p.DrawPass1 = quad;

        // Auto-free when finished.
        p.Emitting = true;
        var tw = CreateTween();
        tw.TweenInterval(p.Lifetime + 0.6);
        tw.TweenCallback(Callable.From(() => p.QueueFree()));
        return p;
    }
}
