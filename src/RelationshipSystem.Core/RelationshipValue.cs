namespace RelationshipSystem.Core;

/// <summary>
/// Representa o relacionamento de UMA direção: como A vê B.
/// Possui dois eixos independentes — <see cref="Daily"/> (volátil) e
/// <see cref="Lifetime"/> (estável). Ambos limitados a [-100, +100].
/// </summary>
public sealed class RelationshipValue
{
    /// <summary>-100 (ódio total) a +100 (amor total). Curto prazo, volátil.</summary>
    public float Daily { get; private set; } = 0f;

    /// <summary>-100 (ódio total) a +100 (amor total). Longo prazo, estável.</summary>
    public float Lifetime { get; private set; } = 0f;

    /// <summary>Aplica um delta ao daily (interações, decaimento).</summary>
    public void ApplyDaily(float delta)
    {
        Daily = Clamp(Daily + delta);
    }

    /// <summary>Aplica um delta ao lifetime (interações duradouras).</summary>
    public void ApplyLifetime(float delta)
    {
        Lifetime = Clamp(Lifetime + delta);
    }

    /// <summary>
    /// Normaliza lifetime em direção ao daily (The Sims 2: +3 por passo).
    /// Se a diferença for menor que o passo, iguala os dois.
    /// O lifetime NÃO decai por tempo — apenas persegue o daily.
    /// </summary>
    public void NormalizeLifetimeTowardDaily(float step)
    {
        if (step < 0f)
            throw new ArgumentOutOfRangeException(nameof(step), step, "step must be >= 0");

        float diff = Daily - Lifetime;
        if (Math.Abs(diff) <= step)
        {
            Lifetime = Daily;
            return;
        }
        Lifetime = Clamp(Lifetime + Math.Sign(diff) * step);
    }

    /// <summary>
    /// Decai daily em direção a zero (The Sims 2: -2/dia).
    /// Simulação de "falta de contato".
    /// </summary>
    public void DecayDailyTowardZero(float step)
    {
        if (step < 0f)
            throw new ArgumentOutOfRangeException(nameof(step), step, "step must be >= 0");

        if (Math.Abs(Daily) <= step)
        {
            Daily = 0f;
            return;
        }
        Daily -= Math.Sign(Daily) * step;
    }

    /// <summary>Esvazia os valores (para reset ou debug).</summary>
    public void Reset()
    {
        Daily = 0f;
        Lifetime = 0f;
    }

    private static float Clamp(float value) => Math.Clamp(value, -100f, 100f);
}
