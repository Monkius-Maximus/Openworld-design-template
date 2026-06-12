namespace MovementSystem.Core;

/// <summary>
/// Constantes de tuning da movimentação 3D estilo Project Zomboid
/// (mesmo papel de RelationshipThresholds/EconomyThresholds).
/// Unidades: metros, segundos, radianos. Convenção de eixos do Godot:
/// Y para cima, chão no plano XZ, "frente" = -Z quando yaw = 0.
/// </summary>
public static class MovementThresholds
{
    // ---- velocidades por postura (m/s) ----
    public const float SneakSpeed = 1.6f;
    public const float WalkSpeed = 3.2f;
    public const float RunSpeed = 6.0f;
    public const float SprintSpeed = 8.5f;

    /// <summary>Andando de arma erguida (PZ: mirar trava num passo lento).</summary>
    public const float AimWalkSpeed = 2.2f;

    // ---- aceleração horizontal (m/s²) ----
    public const float Acceleration = 18f;
    public const float Deceleration = 24f;

    // ---- penalidade direcional ao mirar (PZ: recuar é mais lento) ----
    /// <summary>Multiplicador ao mover-se de lado em relação à mira.</summary>
    public const float StrafeMultiplier = 0.8f;

    /// <summary>Multiplicador ao mover-se de costas para a mira.</summary>
    public const float BackpedalMultiplier = 0.5f;

    // ---- rotação do personagem (rad/s) ----
    public const float TurnSpeedRadians = 3f * MathF.PI;       // 540°/s
    public const float AimTurnSpeedRadians = 5f * MathF.PI;    // 900°/s (mira responde rápido)

    // ---- câmera ----
    /// <summary>Rigidez da suavização exponencial do follow (maior = mais colada).</summary>
    public const float CameraFollowSharpness = 6f;

    /// <summary>Size mínimo/máximo da câmera ortográfica (menor = mais perto).</summary>
    public const float CameraMinSize = 6f;
    public const float CameraMaxSize = 28f;

    /// <summary>Fator multiplicativo por "clique" de zoom no scroll.</summary>
    public const float CameraZoomStepFactor = 1.15f;

    /// <summary>Velocidade da rotação da câmera entre passos de 45° (rad/s).</summary>
    public const float CameraRotateSpeedRadians = 1.5f * MathF.PI; // 270°/s

    // ---- look-ahead da mira (PZ: a câmera desliza rumo ao cursor ao mirar) ----
    /// <summary>Fração da distância jogador→cursor que a âncora da câmera avança.</summary>
    public const float AimLookAheadFactor = 0.35f;

    /// <summary>Deslocamento máximo da âncora ao mirar (m).</summary>
    public const float AimLookAheadMax = 4f;
}
