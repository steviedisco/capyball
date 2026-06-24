using Godot;

namespace Capyball;

/// <summary>
/// The runtime host for a single level. Given a LevelSpec, it instantiates all
/// geometry, props, the player + camera, wires FX, and drives the gameplay loop
/// (timer, melon count, fall detection, win/lose/restart). HUD is overlaid.
/// </summary>
public partial class LevelScene : Node3D
{
    public LevelSpec Spec { get; private set; }
    public CapyballBall Player { get; private set; }
    public CameraFollow Cam { get; private set; }
    public Hud Hud { get; private set; }

    public float Elapsed { get; private set; }
    public int MelonsTotal { get; private set; }
    public int MelonsCollected { get; private set; }
    public bool Finished { get; private set; }

    private Goal _goal;
    private bool _fell;

    public void Load(string id)
    {
        Spec = Levels.Get(id);
        GameState.Instance.CurrentLevelId = id;
        Build();
    }

    private void Build()
    {
        Name = "LevelScene";

        // Environment + lighting + post FX.
        Stage.BuildEnvironment(this, Spec.SkyTop, Spec.SkyBottom);
        Stage.BuildVoidPlane(this, y: -22f, size: 600f);
        Stage.AddDistantSparkles(this, count: 18, radius: 80f, height: 30f);

        // Static platforms.
        foreach (var c in Spec.Chunks)
        {
            var box = Procedural.PlatformBox(c.Size, c.Color, c.Emission);
            box.Position = c.Pos;
            var body = new StaticBody3D();
            body.AddChild(box);
            var col = new CollisionShape3D { Shape = new BoxShape3D { Size = c.Size } };
            body.AddChild(col);
            AddChild(body);
        }

        // Ramps (rotated boxes).
        foreach (var r in Spec.Ramps)
        {
            var box = Procedural.PlatformBox(r.Size, r.Color, 0.3f);
            box.Position = r.Pos;
            var body = new StaticBody3D();
            body.AddChild(box);
            var col = new CollisionShape3D { Shape = new BoxShape3D { Size = r.Size } };
            body.AddChild(col);
            body.RotationDegrees = new Vector3(r.PitchDeg, r.YawDeg, 0);
            AddChild(body);
        }

        // Moving platforms.
        foreach (var m in Spec.Movers)
        {
            var mp = new MovingPlatform
            {
                PointA = m.A, PointB = m.B, Period = m.Period,
                Tint = m.Tint, Size = m.Size,
            };
            AddChild(mp);
            mp.GlobalPosition = m.A;
        }

        // Melons.
        MelonsTotal = Spec.Melons.Count;
        foreach (var ml in Spec.Melons)
        {
            var melon = new Melon { Position = ml.Pos };
            AddChild(melon);
        }

        // Boost pads.
        foreach (var b in Spec.Boosts)
        {
            var pad = new BoostPad { Direction = b.Dir, Force = b.Force, Tint = b.Tint, Position = b.Pos };
            AddChild(pad);
        }

        // Bumpers.
        foreach (var bp in Spec.Bumpers)
        {
            var bump = new Bumper { Tint = bp.Tint, Position = bp.Pos };
            AddChild(bump);
        }

        // Goal.
        _goal = new Goal { Position = Spec.Goal, NextLevelId = Spec.NextId };
        AddChild(_goal);

        // Player.
        Player = new CapyballBall();
        AddChild(Player);
        Player.GlobalPosition = Spec.Start + new Vector3(0, 1.0f, 0);

        // Camera.
        Cam = new CameraFollow();
        AddChild(Cam);
        Cam.GlobalPosition = Spec.Start + new Vector3(0, 6, 9);
        Cam.TargetPath = Cam.GetPathTo(Player);
        Player.BindCamera(Cam);
        Cam.Current = true;

        // HUD.
        Hud = Hud.Create();
        AddChild(Hud);
        Hud.Bind(this);
        Hud.SetTitle(Spec.Title, Spec.Subtitle);
        Hud.UpdateMelons(MelonsCollected, MelonsTotal);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Finished) return;

        Elapsed += (float)delta;

        // Fall detection — below the void threshold.
        if (Player.GlobalPosition.Y < -16f && !_fell)
        {
            _fell = true;
            OnFall();
        }

        Hud.UpdateTimer(Elapsed);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("restart"))
        {
            Restart();
        }
        else if (@event.IsActionPressed("pause"))
        {
            // Pause handled in Main, but a quick restart-from-pause is nice.
        }
    }

    public void OnMelonCollected(Melon m)
    {
        MelonsCollected++;
        Hud.UpdateMelons(MelonsCollected, MelonsTotal);
        Fx.Instance?.Shake(0.08f);
    }

    public void OnGoalReached(Goal g)
    {
        if (Finished) return;
        Finished = true;
        GameState.Instance.MarkCompleted(Spec.Id, Elapsed);
        bool last = Levels.NextOf(Spec.Id) == null;
        Hud.ShowWin(Elapsed, MelonsCollected, MelonsTotal, last);
    }

    private void OnFall()
    {
        Finished = true;
        Synth.Instance?.Fall();
        Fx.Instance?.Shake(0.5f);
        Hud.ShowLose();
    }

    public void Restart()
    {
        Synth.Instance?.Click();
        // Reload same level fresh.
        GetTree().ReloadCurrentScene();
        // Note: ReloadCurrentScene reloads the scene file; we instead rebuild via Main.
    }

    public void GoNext()
    {
        string next = Levels.NextOf(Spec.Id);
        if (next == null)
        {
            // Back to menu.
            Main.Instance.GotoMenu();
        }
        else
        {
            Main.Instance.GotoLevel(next);
        }
    }
}
