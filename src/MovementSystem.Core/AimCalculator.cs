using System.Numerics;

namespace MovementSystem.Core;

/// <summary>
/// Matemática da mira estilo Project Zomboid: o cursor do mouse vira um raio
/// da câmera e é projetado num <b>plano horizontal na altura do personagem</b>
/// — não num raycast físico contra o chão. É a técnica recomendada na
/// discussão "How to create an aim system like Project Zomboid" (Unity
/// Discussions): o plano matemático evita que a mira "pule" ao passar o
/// cursor por cima de obstáculos.
/// Convenções: Y para cima; yaw em radianos; frente = -Z quando yaw = 0
/// (igual ao Node3D do Godot, então o resultado pode ir direto em Rotation.Y).
/// </summary>
public static class AimCalculator
{
    public const float Epsilon = 1e-5f;

    /// <summary>
    /// Interseção raio × plano horizontal (y = <paramref name="planeY"/>).
    /// Falha se o raio for paralelo ao plano ou se a interseção ficar atrás da origem.
    /// </summary>
    public static bool TryIntersectPlane(Vector3 rayOrigin, Vector3 rayDirection, float planeY, out Vector3 hit)
    {
        hit = default;
        if (MathF.Abs(rayDirection.Y) < Epsilon)
            return false;

        float t = (planeY - rayOrigin.Y) / rayDirection.Y;
        if (t <= 0f)
            return false;

        hit = rayOrigin + rayDirection * t;
        return true;
    }

    /// <summary>Yaw que faz a frente (-Z) apontar para <paramref name="direction"/> (achatada no plano XZ).</summary>
    public static float YawFromDirection(Vector3 direction)
        => MathF.Atan2(-direction.X, -direction.Z);

    /// <summary>
    /// Yaw para <paramref name="from"/> encarar <paramref name="target"/>.
    /// Falha se os pontos coincidirem no plano XZ (yaw indefinido).
    /// </summary>
    public static bool TryYawTowards(Vector3 from, Vector3 target, out float yaw)
    {
        var flat = new Vector3(target.X - from.X, 0f, target.Z - from.Z);
        if (flat.LengthSquared() < Epsilon * Epsilon)
        {
            yaw = 0f;
            return false;
        }

        yaw = YawFromDirection(flat);
        return true;
    }

    /// <summary>Normaliza um ângulo para (-π, π].</summary>
    public static float WrapAngle(float angle)
    {
        float wrapped = MathF.IEEERemainder(angle, 2f * MathF.PI);
        return wrapped <= -MathF.PI ? wrapped + 2f * MathF.PI : wrapped;
    }

    /// <summary>
    /// Move <paramref name="current"/> rumo a <paramref name="target"/> pelo
    /// caminho angular mais curto, limitado a <paramref name="maxDelta"/> rad.
    /// </summary>
    public static float MoveTowardAngle(float current, float target, float maxDelta)
    {
        float diff = WrapAngle(target - current);
        if (MathF.Abs(diff) <= maxDelta)
            return WrapAngle(target);
        return WrapAngle(current + MathF.Sign(diff) * maxDelta);
    }
}
