namespace RelationshipSystem.Core;

/// <summary>Polaridade de um <see cref="Sentiment"/>: bom ou ruim.</summary>
public enum SentimentPolarity { Positive, Negative }

/// <summary>
/// Tipos de sentimento, espelhando os oito do The Sims 4.
/// Positivos: Close, Adoring, Enamored, Motivated.
/// Negativos: Bitter, Hurt, Guilty, Resentful.
/// <para>
/// <c>Resentful</c> ocupa o lugar do "Furious" do TS4 para não colidir com o
/// modificador temporário de fúria (<see cref="Relationship.FuryModifierName"/>),
/// que é outro conceito (buff volátil, não um sentimento persistente).
/// </para>
/// </summary>
public enum SentimentType
{
    // Positivos
    Close, Adoring, Enamored, Motivated,
    // Negativos
    Bitter, Hurt, Guilty, Resentful
}

/// <summary>
/// Um sentimento persistente e direcional de A por B (estilo The Sims 4).
/// É o meio-termo entre um <see cref="RelationshipModifier"/> (volátil, numérico)
/// e uma memória (permanente): tem categoria, intensidade acumulável e pode ser
/// de curto prazo (decai) ou longo prazo (não decai por tempo).
/// <para>
/// Diferente dos modificadores, sentimentos NÃO entram no score efetivo do
/// relacionamento — são uma camada narrativa/emocional separada, consultável
/// por quem reage ao relacionamento.
/// </para>
/// </summary>
public sealed class Sentiment
{
    public required SentimentType Type { get; init; }

    /// <summary>
    /// Magnitude do sentimento (sempre positiva). Usada para reforço (acúmulo)
    /// e para decidir qual sentimento é descartado quando se excede o limite.
    /// </summary>
    public float Intensity { get; set; } = 1f;

    /// <summary>Longo prazo não decai por tempo (ex.: traição, casamento).</summary>
    public bool IsLongTerm { get; init; }

    /// <summary>Tempo restante em horas (ignorado quando <see cref="IsLongTerm"/>).</summary>
    public float RemainingHours { get; set; }

    public SentimentPolarity Polarity => Classify(Type);

    /// <summary>Expira apenas se for de curto prazo e o tempo zerar.</summary>
    public bool IsExpired => !IsLongTerm && RemainingHours <= 0f;

    /// <summary>
    /// Envelhece um sentimento de curto prazo. Longo prazo é imune.
    /// Retorna true se expirou neste tick.
    /// </summary>
    public bool DecayTime(float hoursPassed)
    {
        if (hoursPassed < 0f)
            throw new ArgumentOutOfRangeException(nameof(hoursPassed), hoursPassed, "hoursPassed must be >= 0");

        if (IsLongTerm)
            return false;

        RemainingHours -= hoursPassed;
        return IsExpired;
    }

    private static SentimentPolarity Classify(SentimentType type) => type switch
    {
        SentimentType.Close or SentimentType.Adoring
            or SentimentType.Enamored or SentimentType.Motivated => SentimentPolarity.Positive,
        _ => SentimentPolarity.Negative
    };
}
