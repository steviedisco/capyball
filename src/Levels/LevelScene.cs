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

    // The tilt feel — how steep and how snappy the course banks under input.
    public float MaxTiltDeg = 26f;
    public float TiltSpeed = 8f;

    private Goal _goal;
    private bool _fell;

    // Everything that banks with the course (geometry + props) lives under this
    // pivot. The ball + camera + environment are NOT children, so the camera
    // stays upright while the world visibly tilts — authentic Super Monkey Ball.
    private Node3D _tilt;
    private float _tiltX, _tiltZ;

    public void Load(string id)
    {
        Spec = Levels.Get(id);
        GameState.Instance.CurrentLevelId = id;
        Build();
    }

    private void Build()
    {
        Name = "LevelScene";

        // The tilt pivot — all course geometry is parented to this.
        _tilt = new Node3D { Name = "TiltPivot" };
        AddChild(_tilt);

        // Environment + lighting + post FX stay OUTSIDE the pivot (upright).
        Stage.BuildEnvironment(this, Spec.SkyTop, Spec.SkyBottom);
        Stage.BuildVoidPlane(this, y: -22f, size: 600f);
        Stage.AddDistantSparkles(this, count: 18, radius: 80f, height: 30f);
        Stage.AddClouds(this, count: 7, radius: 90f, height: 45f);

        // Static platforms — under the tilt pivot.
        foreach (var c in Spec.Chunks)
        {
            var (faceMat, rimMat) = PlatformMaterials(c.Color, c.Emission, c.Size);
            var box = Procedural.PlatformBox(c.Size, faceMat, rimMat);
            var body = new StaticBody3D();
            body.AddChild(box);
            var col = new CollisionShape3D { Shape = new BoxShape3D { Size = c.Size } };
            body.AddChild(col);
            _tilt.AddChild(body);
            body.GlobalPosition = c.Pos;
        }

        // Ramps (rotated boxes).
        foreach (var r in Spec.Ramps)
        {
            var (faceMat, rimMat) = PlatformMaterials(r.Color, 0.3f, r.Size);
            var box = Procedural.PlatformBox(r.Size, faceMat, rimMat);
            var body = new StaticBody3D();
            body.AddChild(box);
            var col = new CollisionShape3D { Shape = new BoxShape3D { Size = r.Size } };
            body.AddChild(col);
            body.RotationDegrees = new Vector3(r.PitchDeg, r.YawDeg, 0);
            _tilt.AddChild(body);
            body.GlobalPosition = r.Pos;
        }

        // Perimeter walls — guardrail look: dark body + bright cap rail.
        foreach (var w in Spec.Walls)
        {
            var bodyMat = Assets.MaterialTinted("wall", w.Color);
            Procedural.SetUvScale(bodyMat, new Vector3(w.Size.X / 4f, 1, w.Size.Z / 4f));
            var railMat = Assets.MaterialTinted("platform_rim",
                new Color(Mathf.Min(1, w.Color.R + 0.3f), Mathf.Min(1, w.Color.G + 0.3f), Mathf.Min(1, w.Color.B + 0.3f)));
            var seg = Procedural.WallSegment(w.Size, bodyMat, railMat);
            var body = new StaticBody3D();
            body.AddChild(seg);
            var col = new CollisionShape3D { Shape = new BoxShape3D { Size = w.Size } };
            body.AddChild(col);
            _tilt.AddChild(body);
            body.GlobalPosition = w.Pos;
        }

        // Moving platforms.
        foreach (var m in Spec.Movers)
        {
            var mp = new MovingPlatform
            {
                PointA = m.A, PointB = m.B, Period = m.Period,
                Tint = m.Tint, Size = m.Size,
            };
            _tilt.AddChild(mp);
            mp.GlobalPosition = m.A;
        }

        // Melons.
        MelonsTotal = Spec.Melons.Count;
        foreach (var ml in Spec.Melons)
        {
            var melon = new Melon { Position = ml.Pos };
            _tilt.AddChild(melon);
        }

        // Boost pads.
        foreach (var b in Spec.Boosts)
        {
            var pad = new BoostPad { Direction = b.Dir, Force = b.Force, Tint = b.Tint, Position = b.Pos };
            _tilt.AddChild(pad);
        }

        // Bumpers.
        foreach (var bp in Spec.Bumpers)
        {
            var bump = new Bumper { Tint = bp.Tint, Position = bp.Pos };
            _tilt.AddChild(bump);
        }

        // Goal.
        _goal = new Goal { Position = Spec.Goal, NextLevelId = Spec.NextId };
        _tilt.AddChild(_goal);

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

        // Read tilt input (camera-relative) and bank the course geometry. The ball
        // sits outside the pivot, so standard world-down gravity then rolls it down
        // the real, visible slope — authentic Super Monkey Ball.
        UpdateTilt((float)delta);

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

    /// <summary>Read tilt input (camera-relative), smooth toward the target, and bank
    /// the course pivot. The ball + camera sit outside the pivot, so the camera
    /// stays upright while the world visibly tilts and standard gravity rolls the
    /// ball down the real slope.</summary>
    private void UpdateTilt(float dt)
    {
        // Input vector: Y = forward/back, X = left/right.
        Vector2 input = Vector2.Zero;
        if (Input.IsActionPressed("move_left")) input.X -= 1;
        if (Input.IsActionPressed("move_right")) input.X += 1;
        if (Input.IsActionPressed("move_forward")) input.Y += 1;
        if (Input.IsActionPressed("move_back")) input.Y -= 1;
        if (input.LengthSquared() > 1f) input = input.Normalized();

        // Target tilt angles (radians). forward input banks the course so its far
        // edge drops (ball rolls away from camera); right input banks it right.
        float maxTilt = Mathf.DegToRad(MaxTiltDeg);
        float targetPitch = input.Y * maxTilt;
        float targetRoll = input.X * maxTilt;

        float k = 1f - Mathf.Exp(-TiltSpeed * dt);
        _tiltX = Mathf.Lerp(_tiltX, targetPitch, k);
        _tiltZ = Mathf.Lerp(_tiltZ, targetRoll, k);

        // Apply the bank around the BALL's position, not the world origin — otherwise
        // a rotation sweeps distant geometry sideways and pulls the floor out from
        // under the ball. Transform = translateToPivot * rotate * translateBack.
        Vector3 camRight = Cam != null ? Cam.RightFlat : Vector3.Right;
        Vector3 camFwd = Cam != null ? Cam.ForwardFlat : Vector3.Forward;

        var bank = new Basis(camRight, _tiltX) * new Basis(camFwd, _tiltZ);
        bank = bank.Orthonormalized();

        // Pivot around the ball's ground point so the surface stays roughly under it
        // while the slope forms. This keeps the ball on the course as it banks.
        Vector3 pivot = Player != null ? new Vector3(Player.GlobalPosition.X, 0, Player.GlobalPosition.Z) : Vector3.Zero;
        _tilt.GlobalTransform = new Transform3D(Basis.Identity, pivot)
                              * new Transform3D(bank, Vector3.Zero)
                              * new Transform3D(Basis.Identity, -pivot);
    }

    /// <summary>Builds a tinted face + brighter rim material pair for a platform,
    /// each an independent instance so per-chunk colour never bleeds across platforms.
    /// UV tiling is set so the checker texture repeats roughly every 4 world units.</summary>
    private static (Material face, Material rim) PlatformMaterials(Color color, float emission, Vector3 size)
    {
        var faceMat = Assets.MaterialTinted("platform", color);
        faceMat.EmissionEnergyMultiplier = emission;
        Procedural.SetUvScale(faceMat, new Vector3(size.X / 4f, size.Y / 4f, size.Z / 4f));
        Color rimColor = new(
            Mathf.Min(1, color.R + 0.25f), Mathf.Min(1, color.G + 0.25f), Mathf.Min(1, color.B + 0.25f));
        var rimMat = Assets.MaterialTinted("platform_rim", rimColor);
        return (faceMat, rimMat);
    }
}
