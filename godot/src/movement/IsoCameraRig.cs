using Godot;
using MovementSystem.Core;

namespace BuildSystem.Movement;

/// <summary>
/// Rig de câmera isométrica estilo Project Zomboid: segue o jogador com
/// suavização exponencial, zoom por scroll (câmera ortográfica) e, ao mirar,
/// desliza o enquadramento rumo ao cursor. Q/E giram a vista em passos de 45°
/// (extra em relação ao PZ, que tem ângulo fixo).
/// Estrutura esperada: Rig (este nó, só yaw) → Pivot (pitch) → Camera3D.
/// </summary>
public partial class IsoCameraRig : Node3D
{
    [Export] public PlayerController3D? Target { get; set; }
    [Export] public Camera3D? Camera { get; set; }
    [Export] public float FollowSharpness { get; set; } = MovementThresholds.CameraFollowSharpness;
    [Export] public float RotateStepDegrees { get; set; } = 45f;

    /// <summary>Yaw atual (radianos) — o player move relativo a este valor.</summary>
    public float YawRadians => Rotation.Y;

    private float _targetYaw;

    public override void _Ready()
    {
        _targetYaw = Rotation.Y;
        if (Target is not null)
            GlobalPosition = Target.GlobalPosition;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("camera_zoom_in"))
            ApplyZoom(+1);
        else if (@event.IsActionPressed("camera_zoom_out"))
            ApplyZoom(-1);
        else if (@event.IsActionPressed("camera_rotate_left"))
            _targetYaw += Mathf.DegToRad(RotateStepDegrees);
        else if (@event.IsActionPressed("camera_rotate_right"))
            _targetYaw -= Mathf.DegToRad(RotateStepDegrees);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        float yaw = AimCalculator.MoveTowardAngle(
            Rotation.Y, _targetYaw, MovementThresholds.CameraRotateSpeedRadians * dt);
        Rotation = new Vector3(0f, yaw, 0f);
        _targetYaw = AimCalculator.WrapAngle(_targetYaw);

        if (Target is null)
            return;

        Vector3 anchor = Target.GlobalPosition;
        if (Target.IsAiming)
        {
            System.Numerics.Vector3 shifted = CameraRigCalculator.AimAnchor(
                ToSys(Target.GlobalPosition), ToSys(Target.AimPoint),
                MovementThresholds.AimLookAheadFactor, MovementThresholds.AimLookAheadMax);
            anchor = new Vector3(shifted.X, shifted.Y, shifted.Z);
        }

        System.Numerics.Vector3 pos = CameraRigCalculator.SmoothFollow(
            ToSys(GlobalPosition), ToSys(anchor), FollowSharpness, dt);
        GlobalPosition = new Vector3(pos.X, pos.Y, pos.Z);
    }

    private void ApplyZoom(int steps)
    {
        if (Camera is not null)
            Camera.Size = CameraRigCalculator.ApplyZoomSteps(Camera.Size, steps);
    }

    private static System.Numerics.Vector3 ToSys(Vector3 v) => new(v.X, v.Y, v.Z);
}
