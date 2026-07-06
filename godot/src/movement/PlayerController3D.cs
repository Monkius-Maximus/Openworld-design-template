using Godot;
using MovementSystem.Core;

namespace BuildSystem.Movement;

/// <summary>
/// Movimentação 3D estilo Project Zomboid: WASD relativo à câmera, posturas
/// (sneak C / run Shift / sprint Alt) e mira com o botão direito — o cursor é
/// projetado num plano horizontal na altura do personagem (AimCalculator),
/// que gira para encará-lo; recuar mirando é mais lento. Toda a matemática
/// vive em MovementSystem.Core; aqui só entra I/O da engine.
/// </summary>
public partial class PlayerController3D : CharacterBody3D
{
    /// <summary>
    /// Único gancho para suspender o controle (mesmo padrão do Player 2D, D25).
    /// </summary>
    public bool ControlEnabled { get; set; } = true;

    [Export] public IsoCameraRig? CameraRig { get; set; }

    /// <summary>Indicador visual do ponto de mira (opcional).</summary>
    [Export] public Node3D? AimMarker { get; set; }

    /// <summary>
    /// Altura do plano de mira acima dos pés. 0 mira no nível do chão; com os
    /// assets finais, ajuste para a altura do peito/arma do modelo.
    /// </summary>
    [Export] public float AimPlaneHeight { get; set; }

    public bool IsAiming { get; private set; }
    public Vector3 AimPoint { get; private set; }

    private readonly float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity").AsDouble();

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 velocity = Velocity;
        if (!IsOnFloor())
            velocity.Y -= _gravity * dt;

        if (!ControlEnabled)
        {
            var stopped = MovementCalculator.Accelerate(
                new System.Numerics.Vector3(velocity.X, 0f, velocity.Z), System.Numerics.Vector3.Zero, dt);
            Velocity = new Vector3(stopped.X, velocity.Y, stopped.Z);
            MoveAndSlide();
            SetAiming(false);
            return;
        }

        Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool aimHeld = Input.IsActionPressed("aim");
        MovementStance stance = StanceResolver.Resolve(
            sneak: Input.IsActionPressed("sneak"),
            run: Input.IsActionPressed("run"),
            sprint: Input.IsActionPressed("sprint"),
            aiming: aimHeld);

        float cameraYaw = CameraRig?.YawRadians ?? 0f;
        System.Numerics.Vector3 moveDir = MovementCalculator.DirectionFromInput(
            new System.Numerics.Vector2(input.X, input.Y), cameraYaw);

        bool aiming = false;
        if (aimHeld && TryProjectMouseOnAimPlane(out Vector3 aimPoint))
        {
            aiming = true;
            AimPoint = aimPoint;
        }
        SetAiming(aiming);

        // rotação: mirando encara o cursor; senão, encara a direção do movimento
        float yaw = Rotation.Y;
        if (aiming && AimCalculator.TryYawTowards(ToSys(GlobalPosition), ToSys(AimPoint), out float aimYaw))
            yaw = AimCalculator.MoveTowardAngle(yaw, aimYaw, MovementThresholds.AimTurnSpeedRadians * dt);
        else if (moveDir.LengthSquared() > 0f)
            yaw = AimCalculator.MoveTowardAngle(yaw, AimCalculator.YawFromDirection(moveDir), MovementThresholds.TurnSpeedRadians * dt);
        Rotation = new Vector3(0f, yaw, 0f);

        float speed = StanceResolver.SpeedFor(stance, aiming);
        if (aiming)
            speed *= MovementCalculator.DirectionalSpeedMultiplier(yaw, moveDir);

        System.Numerics.Vector3 horizontal = MovementCalculator.Accelerate(
            new System.Numerics.Vector3(velocity.X, 0f, velocity.Z), moveDir * speed, dt);
        Velocity = new Vector3(horizontal.X, velocity.Y, horizontal.Z);
        MoveAndSlide();
    }

    /// <summary>
    /// Projeta o cursor num plano matemático na altura configurada (técnica da
    /// discussão da Unity sobre o aim do PZ — sem raycast físico no cenário).
    /// </summary>
    private bool TryProjectMouseOnAimPlane(out Vector3 point)
    {
        point = default;
        Camera3D? camera = GetViewport().GetCamera3D();
        if (camera is null)
            return false;

        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 origin = camera.ProjectRayOrigin(mouse);
        Vector3 direction = camera.ProjectRayNormal(mouse);

        if (!AimCalculator.TryIntersectPlane(
                ToSys(origin), ToSys(direction), GlobalPosition.Y + AimPlaneHeight, out System.Numerics.Vector3 hit))
            return false;

        point = new Vector3(hit.X, hit.Y, hit.Z);
        return true;
    }

    private void SetAiming(bool aiming)
    {
        IsAiming = aiming;
        if (AimMarker is null)
            return;

        AimMarker.Visible = aiming;
        if (aiming)
            AimMarker.GlobalPosition = new Vector3(AimPoint.X, GlobalPosition.Y + 0.05f, AimPoint.Z);
    }

    private static System.Numerics.Vector3 ToSys(Vector3 v) => new(v.X, v.Y, v.Z);
}
