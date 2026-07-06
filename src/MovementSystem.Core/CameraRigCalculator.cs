using System.Numerics;

namespace MovementSystem.Core;

/// <summary>
/// Matemática do rig de câmera isométrica estilo Project Zomboid: follow
/// suavizado, zoom ortográfico em passos e look-ahead rumo ao cursor ao mirar.
/// </summary>
public static class CameraRigCalculator
{
    public const float Epsilon = 1e-5f;

    /// <summary>
    /// Suavização exponencial independente de frame rate: quanto maior a
    /// sharpness, mais "colada" a câmera fica no alvo.
    /// </summary>
    public static Vector3 SmoothFollow(Vector3 current, Vector3 target, float sharpness, float deltaSeconds)
        => target + (current - target) * MathF.Exp(-sharpness * deltaSeconds);

    /// <summary>
    /// Âncora da câmera ao mirar: desloca uma fração da distância
    /// jogador→cursor (achatada no plano XZ), limitada a <paramref name="maxOffset"/>.
    /// É o "lean" de câmera do PZ ao erguer a arma.
    /// </summary>
    public static Vector3 AimAnchor(Vector3 playerPosition, Vector3 aimPoint, float factor, float maxOffset)
    {
        var offset = new Vector3(aimPoint.X - playerPosition.X, 0f, aimPoint.Z - playerPosition.Z) * factor;
        float length = offset.Length();
        if (length > maxOffset && length > Epsilon)
            offset *= maxOffset / length;
        return playerPosition + offset;
    }

    /// <summary>
    /// Aplica passos de zoom à câmera ortográfica. steps &gt; 0 aproxima
    /// (size menor); o resultado é limitado a [min, max].
    /// </summary>
    public static float ApplyZoomSteps(
        float currentSize,
        int steps,
        float min = MovementThresholds.CameraMinSize,
        float max = MovementThresholds.CameraMaxSize,
        float stepFactor = MovementThresholds.CameraZoomStepFactor)
        => Math.Clamp(currentSize / MathF.Pow(stepFactor, steps), min, max);
}
