using System.Numerics;

namespace MovementSystem.Core;

/// <summary>
/// Locomoção no plano XZ: input relativo à câmera, penalidade direcional ao
/// mirar (PZ: recuar de arma erguida é lento) e aceleração/desaceleração.
/// </summary>
public static class MovementCalculator
{
    public const float Epsilon = 1e-5f;

    /// <summary>
    /// Converte o input 2D em direção de mundo no plano XZ, girada pelo yaw da
    /// câmera ("cima na tela" sempre afasta da câmera). Convenção idêntica ao
    /// Input.GetVector do Godot: input.X = direita, input.Y = baixo.
    /// </summary>
    public static Vector3 DirectionFromInput(Vector2 input, float cameraYaw)
    {
        if (input.LengthSquared() < Epsilon * Epsilon)
            return Vector3.Zero;
        if (input.LengthSquared() > 1f)
            input = Vector2.Normalize(input);

        float cos = MathF.Cos(cameraYaw);
        float sin = MathF.Sin(cameraYaw);
        return new Vector3(
            input.X * cos + input.Y * sin,
            0f,
            -input.X * sin + input.Y * cos);
    }

    /// <summary>
    /// Multiplicador de velocidade conforme o ângulo entre a direção encarada
    /// (yaw) e a direção do movimento: 1 de frente, StrafeMultiplier de lado,
    /// BackpedalMultiplier de costas — com transição suave entre eles.
    /// </summary>
    public static float DirectionalSpeedMultiplier(float facingYaw, Vector3 moveDirection)
    {
        var flat = new Vector3(moveDirection.X, 0f, moveDirection.Z);
        if (flat.LengthSquared() < Epsilon * Epsilon)
            return 1f;

        var forward = new Vector3(-MathF.Sin(facingYaw), 0f, -MathF.Cos(facingYaw));
        float dot = Vector3.Dot(forward, Vector3.Normalize(flat));

        return dot >= 0f
            ? Lerp(MovementThresholds.StrafeMultiplier, 1f, dot)
            : Lerp(MovementThresholds.StrafeMultiplier, MovementThresholds.BackpedalMultiplier, -dot);
    }

    /// <summary>
    /// Aproxima a velocidade horizontal atual da desejada sem ultrapassá-la,
    /// usando Acceleration quando há intenção de movimento e Deceleration na frenagem.
    /// </summary>
    public static Vector3 Accelerate(Vector3 currentVelocity, Vector3 desiredVelocity, float deltaSeconds)
    {
        float rate = desiredVelocity.LengthSquared() > Epsilon * Epsilon
            ? MovementThresholds.Acceleration
            : MovementThresholds.Deceleration;

        Vector3 delta = desiredVelocity - currentVelocity;
        float maxStep = rate * deltaSeconds;
        if (delta.Length() <= maxStep)
            return desiredVelocity;

        return currentVelocity + Vector3.Normalize(delta) * maxStep;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
