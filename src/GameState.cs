using System.Collections.Generic;
using Godot;

namespace Capyball;

/// <summary>
/// A simple persistent game-state autoload. Tracks unlocks, best times and
/// the current run so levels can hand off cleanly to one another.
/// </summary>
public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    /// <summary>Levels that have been completed at least once.</summary>
    public HashSet<string> Completed { get; } = new();

    /// <summary>Best clear time per level id, in seconds.</summary>
    public Dictionary<string, float> BestTimes { get; } = new();

    public string CurrentLevelId { get; set; } = Level1.LevelId;
    public int LevelIndex { get; set; } = 0;

    public override void _Ready()
    {
        Instance = this;
        // Keep this node (and its data) across scene reloads.
        ProcessMode = ProcessModeEnum.Always;
        Load();
    }

    public void MarkCompleted(string id, float time)
    {
        Completed.Add(id);
        if (!BestTimes.TryGetValue(id, out float prev) || time < prev)
            BestTimes[id] = time;
        Save();
    }

    public bool IsCompleted(string id) => Completed.Contains(id);
    public float GetBest(string id) => BestTimes.TryGetValue(id, out float t) ? t : 0f;

    private const string SavePath = "user://progress.cfg";

    private void Save()
    {
        var cfg = new ConfigFile();
        foreach (var kv in BestTimes)
            cfg.SetValue("times", kv.Key, kv.Value);
        foreach (var id in Completed)
            cfg.SetValue("done", id, true);
        cfg.Save(SavePath);
    }

    private void Load()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) == Error.Ok)
        {
            foreach (string id in cfg.GetSectionKeys("times"))
                BestTimes[id] = (float)cfg.GetValue("times", id);
            foreach (string id in cfg.GetSectionKeys("done"))
                if ((bool)cfg.GetValue("done", id))
                    Completed.Add(id);
        }
    }
}
