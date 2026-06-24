using System.Collections.Generic;
using Godot;

namespace Capyball;

/// <summary>
/// Tiny procedural audio engine. Every sound is synthesized at runtime so the
/// repo ships zero audio assets. Designed for short, punchy, arcade-style SFX.
/// </summary>
public partial class Synth : Node
{
    public static Synth Instance { get; private set; }

    private readonly Stack<AudioStreamPlayer> _pool = new();
    private int _bus;

    public override void _Ready()
    {
        Instance = this;
        // A dedicated SFX bus with a gentle limiter so stacked sounds never clip.
        _bus = AudioServer.GetBusIndex("Sfx");
        if (_bus < 0)
        {
            AudioServer.AddBus();
            _bus = AudioServer.BusCount - 1;
            AudioServer.SetBusName(_bus, "Sfx");
            AudioServer.SetBusSend(_bus, "Master");
            var limiter = new AudioEffectHardLimiter
            {
                CeilingDb = -0.5f,
                PreGainDb = -1.5f,
                Release = 6.0f / 1000.0f, // 6 ms — fast catch-up
            };
            AudioServer.AddBusEffect(_bus, limiter);
        }

        // Master bus gets a subtle reverb-ish for airiness via a short delay.
        int master = AudioServer.GetBusIndex("Master");
        if (master >= 0 && AudioServer.GetBusEffectCount(master) == 0)
        {
            var delay = new AudioEffectDelay
            {
                Dry = 0.0f,
                Tap1Active = true,
                Tap1DelayMs = 110f,
                Tap1LevelDb = -14f,
                FeedbackActive = false,
            };
            AudioServer.AddBusEffect(master, delay);
        }
    }

    public enum Wave { Sine, Square, Saw, Triangle, Noise }

    private AudioStreamPlayer Player()
    {
        if (_pool.TryPop(out var reuse))
        {
            reuse.Stream = null;
            return reuse;
        }
        var player = new AudioStreamPlayer
        {
            Bus = AudioServer.GetBusName(_bus),
            MixTarget = AudioStreamPlayer.MixTargetEnum.Stereo,
        };
        AddChild(player);
        return player;
    }

    private AudioStreamGenerator MakeStream(double duration, int sampleRate = 44100)
    {
        int frames = Mathf.CeilToInt(sampleRate * duration);
        var gen = new AudioStreamGenerator
        {
            MixRate = sampleRate,
            BufferLength = Mathf.Max(0.10f, (float)(duration + 0.05)),
        };
        return gen;
    }

    private delegate void FillFn(float[] buf, int channels, double rate);

    private void PlayFilled(double duration, FillFn fill, float volumeDb = -8f)
    {
        var player = Player();
        var stream = MakeStream(duration);
        player.Stream = stream;
        player.Play();
        var playback = (AudioStreamGeneratorPlayback)player.GetStreamPlayback();
        // AudioStreamGeneratorPlayback is mono by default.
        const int channels = 1;

        int total = Mathf.CeilToInt(stream.MixRate * duration);
        var data = new Vector2[total];
        var single = new float[total];
        fill(single, channels, stream.MixRate);
        for (int i = 0; i < total; i++)
            data[i] = new Vector2(single[i], single[i]);
        if (!playback.PushBuffer(data))
        {
            // fall back frame by frame
            int pushed = 0;
            const int chunk = 256;
            while (pushed < total)
            {
                int take = Mathf.Min(chunk, total - pushed);
                var mini = new Vector2[take];
                for (int i = 0; i < take; i++)
                    mini[i] = data[pushed + i];
                playback.PushBuffer(mini);
                pushed += take;
            }
        }
        player.VolumeDb = volumeDb;
        // Return to pool after playback + a margin.
        var tw = CreateTween();
        tw.TweenInterval(duration + 0.4);
        tw.TweenCallback(Callable.From(() => { player.Stop(); _pool.Push(player); }));
    }

    // ---- Public SFX -------------------------------------------------------

    /// <summary>Rolling rumble — call continuously with a 0..1 speed factor.</summary>
    public void Roll(float speed01)
    {
        // Lightweight: brief filtered noise tick, retriggered by caller cadence.
        if (speed01 < 0.05f) return;
        PlayFilled(0.07, (buf, ch, rate) => NoiseFill(buf, rate, 0.18f + speed01 * 0.5f, 900f + speed01 * 1600f),
            volumeDb: -22f + speed01 * 8f);
    }

    public void Bump(float strength = 1f)
    {
        float f = 320f * Mathf.Lerp(1f, 1.6f, Mathf.Clamp(strength, 0f, 1f));
        PlayFilled(0.16, (buf, ch, rate) => PitchedNoise(buf, rate, f, decay: 0.45, q: 4f),
            volumeDb: -10f);
    }

    public void Land(float strength = 1f)
    {
        PlayFilled(0.22, (buf, ch, rate) => PitchedNoise(buf, rate, 180f, decay: 0.30, q: 2.5f),
            volumeDb: -8f - strength * 3f);
    }

    public void Boost()
    {
        PlayFilled(0.30, (buf, ch, rate) => Sweep(buf, rate, 220f, 1400f, wave: Wave.Square, vol: 0.5f),
            volumeDb: -7f);
    }

    public void Pickup()
    {
        // bright two-note arpeggio
        PlayFilled(0.18, (buf, ch, rate) => Tone(buf, rate, 880f, 0.18, wave: Wave.Triangle, vol: 0.5f), volumeDb: -8f);
        var tw = CreateTween();
        tw.TweenInterval(0.09);
        tw.TweenCallback(Callable.From(() =>
            PlayFilled(0.22, (b, c, r) => Tone(b, r, 1320f, 0.22, wave: Wave.Triangle, vol: 0.5f), volumeDb: -8f)));
    }

    public void Goal()
    {
        PlayFilled(0.45, (buf, ch, rate) => Arp(buf, rate, new[] { 523f, 659f, 784f, 1046f }, 0.10, wave: Wave.Triangle, vol: 0.5f),
            volumeDb: -6f);
    }

    public void Fall()
    {
        PlayFilled(0.5, (buf, ch, rate) => Sweep(buf, rate, 700f, 90f, wave: Wave.Saw, vol: 0.45f),
            volumeDb: -8f);
    }

    public void Click()
    {
        PlayFilled(0.06, (buf, ch, rate) => Tone(buf, rate, 1100f, 0.06, wave: Wave.Square, vol: 0.4f), volumeDb: -12f);
    }

    public void Whoosh()
    {
        PlayFilled(0.28, (buf, ch, rate) => NoiseSweep(buf, rate, 400f, 3000f, vol: 0.3f), volumeDb: -14f);
    }

    // ---- Fillers ----------------------------------------------------------

    private static void Tone(float[] buf, double rate, float freq, double dur, Wave wave, float vol)
    {
        int n = buf.Length;
        for (int i = 0; i < n; i++)
        {
            double t = i / rate;
            double env = Env(t, dur, attack: 0.005, release: dur * 0.6);
            buf[i] = (float)(WaveSample(wave, freq * t) * env * vol);
        }
    }

    private static void Sweep(float[] buf, double rate, float f0, float f1, Wave wave, float vol)
    {
        int n = buf.Length;
        double dur = n / rate;
        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t = i / rate;
            float freq = (float)Mathf.Lerp(f0, f1, t / dur);
            phase += freq / rate;
            double env = Env(t, dur, attack: 0.006, release: dur * 0.4);
            buf[i] = (float)(WaveSample(wave, phase) * env * vol);
        }
    }

    private static void NoiseSweep(float[] buf, double rate, float f0, float f1, float vol)
    {
        int n = buf.Length;
        double dur = n / rate;
        var rng = new RandomNumberGenerator { Seed = (ulong)Time.GetTicksMsec() };
        double lp = 0;
        for (int i = 0; i < n; i++)
        {
            double t = i / rate;
            float cutoff = (float)Mathf.Lerp(f0, f1, t / dur);
            double a = 2 * Mathf.Pi * cutoff / rate;
            lp += a * (rng.Randf() * 2 - 1 - lp);
            double env = Env(t, dur, attack: 0.01, release: dur * 0.5);
            buf[i] = (float)(lp * env * vol);
        }
    }

    private static void NoiseFill(float[] buf, double rate, float vol, float cutoff)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)(Time.GetTicksMsec() * 7919L) };
        double lp = 0;
        double a = 2 * Mathf.Pi * cutoff / rate;
        int n = buf.Length;
        double dur = n / rate;
        for (int i = 0; i < n; i++)
        {
            double t = i / rate;
            lp += a * (rng.Randf() * 2 - 1 - lp);
            double env = Env(t, dur, attack: 0.002, release: dur * 0.7);
            buf[i] = (float)(lp * env * vol);
        }
    }

    private static void PitchedNoise(float[] buf, double rate, float cutoff, double decay, float q)
    {
        // Simple resonant one-pole on noise — gives a punchy body.
        var rng = new RandomNumberGenerator { Seed = (ulong)(Time.GetTicksMsec() * 13L) };
        double lp = 0, bp = 0;
        double a = 2 * Mathf.Pi * cutoff / rate;
        int n = buf.Length;
        double dur = n / rate;
        for (int i = 0; i < n; i++)
        {
            double t = i / rate;
            double input = rng.Randf() * 2 - 1;
            lp += a * (input - lp);
            bp += a / q * (lp - bp);
            double env = Env(t, dur, attack: 0.002, release: dur * decay);
            buf[i] = (float)(bp * env * 0.6);
        }
    }

    private static void Arp(float[] buf, double rate, float[] freqs, double noteDur, Wave wave, float vol)
    {
        int n = buf.Length;
        int perNote = Mathf.CeilToInt(noteDur * rate);
        for (int i = 0; i < n; i++)
        {
            int note = Mathf.Clamp(i / perNote, 0, freqs.Length - 1);
            double t = i / rate;
            double local = (i % perNote) / rate;
            double env = Env(local, noteDur, attack: 0.004, release: noteDur * 0.6);
            double global = Env(t, n / rate, attack: 0.01, release: n / (rate * 3.0));
            buf[i] = (float)(WaveSample(wave, freqs[note] * t) * env * global * vol);
        }
    }

    private static double Env(double t, double dur, double attack, double release)
    {
        if (t < attack) return t / attack;
        if (t > dur - release) return Mathf.Max(0, (dur - t) / release);
        return 1.0;
    }

    private static double WaveSample(Wave w, double phase)
    {
        switch (w)
        {
            case Wave.Sine: return Mathf.Sin((float)(phase * Mathf.Pi * 2));
            case Wave.Square: return Mathf.PosMod(phase, 1.0) < 0.5 ? 1.0 : -1.0;
            case Wave.Saw: return (Mathf.PosMod(phase, 1.0) * 2.0 - 1.0);
            case Wave.Triangle: return 1.0 - 4.0 * Mathf.Abs(Mathf.PosMod(phase, 1.0) - 0.5);
            case Wave.Noise: default: return GD.RandRange(-1.0, 1.0);
        }
    }
}
