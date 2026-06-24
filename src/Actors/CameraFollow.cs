using Godot;

namespace Capyball;

/// <summary>
/// Smooth chase camera with springy follow, lead-ahead based on ball velocity,
/// and a procedural screen-shake hook driven by the Fx autoload. The shake is
/// applied as an offset that decays exponentially — classic juicy feel.
/// </summary>
[Tool]
public partial class CameraFollow : Camera3D
{
    [Export] public NodePath TargetPath;
    [Export] public Vector3 Offset = new(0, 5.5f, 9.5f);
    [Export] public float FollowSmoothing = 6f;
    [Export] public float LookSmoothing = 8f;
    [Export] public float LeadFactor = 0.18f;

    public Vector3 ForwardFlat => _forwardFlat;
    public Vector3 RightFlat => _rightFlat;

    private Node3D _target;
    private Vector3 _forwardFlat = Vector3.Forward;
    private Vector3 _rightFlat = Vector3.Right;

    // Shake state.
    private float _shakeTrauma;
    private float _shakeTime;
    private const float MaxOffset = 0.7f;
    private const float ShakeDecay = 4.0f;

    public override void _Ready()
    {
        if (TargetPath != null) _target = GetNode<Node3D>(TargetPath);
        Fx.Instance.ShakeCamera += AddShake;
        Fx.Instance.TimeFreeze += OnTimeFreeze;
    }

    public override void _ExitTree()
    {
        if (Fx.Instance != null)
        {
            Fx.Instance.ShakeCamera -= AddShake;
            Fx.Instance.TimeFreeze -= OnTimeFreeze;
        }
    }

    private void OnTimeFreeze(float duration, float timescale)
    {
        var tw = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        Engine.TimeScale = timescale;
        tw.TweenInterval(duration);
        tw.TweenCallback(Callable.From(() => Engine.TimeScale = 1.0));
    }

    public void AddShake(float magnitude)
    {
        // Trauma^2 falloff gives a nice non-linear punch.
        _shakeTrauma = Mathf.Clamp(_shakeTrauma + magnitude, 0f, 1f);
    }

    public override void _PhysicsProcess(double deltaDouble)
    {
        if (_target == null) return;
        float dt = (float)deltaDouble;

        Vector3 lead = _target is RigidBody3D rb ? rb.LinearVelocity * LeadFactor : Vector3.Zero;
        Vector3 desired = _target.GlobalPosition + Offset + lead;

        GlobalPosition = GlobalPosition.Lerp(desired, 1f - Mathf.Exp(-FollowSmoothing * dt));

        Vector3 lookAt = _target.GlobalPosition + new Vector3(0, 1.0f, 0);
        Vector3 curForward = -GlobalTransform.Basis.Z;
        Vector3 wantedForward = (lookAt - GlobalPosition).Normalized();
        Vector3 newForward = curForward.Lerp(wantedForward, 1f - Mathf.Exp(-LookSmoothing * dt)).Normalized();
        var lookTarget = GlobalPosition + newForward;
        LookAt(lookTarget, Vector3.Up);

        _forwardFlat = new Vector3(newForward.X, 0, newForward.Z).Normalized();
        _rightFlat = new Vector3(_forwardFlat.Z, 0, -_forwardFlat.X);

        // Apply shake on top of the look.
        _shakeTrauma = Mathf.Max(0, _shakeTrauma - ShakeDecay * dt);
        if (_shakeTrauma > 0.001f)
        {
            _shakeTime += dt * 30f;
            float shake = _shakeTrauma * _shakeTrauma;
            float ox = Mathf.Sin(_shakeTime * 1.7f) * MaxOffset * shake;
            float oy = Mathf.Cos(_shakeTime * 2.3f) * MaxOffset * shake;
            float oz = Mathf.Sin(_shakeTime * 1.1f) * MaxOffset * shake;
            GlobalPosition += new Vector3(ox, oy, oz);
            // Tiny roll for extra crunch.
            Rotation = new Vector3(Rotation.X, Rotation.Y, Mathf.Sin(_shakeTime * 1.5f) * shake * 0.06f);
        }
        else
        {
            Rotation = new Vector3(Rotation.X, Rotation.Y, Mathf.Lerp(Rotation.Z, 0f, 1f - Mathf.Exp(-12f * dt)));
        }
    }
}
