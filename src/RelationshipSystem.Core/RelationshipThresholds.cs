namespace RelationshipSystem.Core;

/// <summary>
/// Valores que definem transições (The Sims 2 default). Tunáveis por projeto.
/// </summary>
public static class RelationshipThresholds
{
    /// <summary>Amizade: daily mútuo &gt;= 50.</summary>
    public const float Friend = 50f;

    /// <summary>Melhor amizade: lifetime mútuo &gt;= 50.</summary>
    public const float BestFriend = 50f;

    /// <summary>Inimigos: daily &lt;= -50.</summary>
    public const float Enemy = -50f;

    /// <summary>Crush: daily &gt;= 70 (unilateral, contexto romântico).</summary>
    public const float Crush = 70f;

    /// <summary>Amor: lifetime &gt;= 70 (unilateral, romântico).</summary>
    public const float Love = 70f;
}

/// <summary>
/// Constantes de decay e normalização (The Sims 2).
/// </summary>
public static class RelationshipPhysics
{
    /// <summary>Daily decai 2 pontos por dia (falta de contato).</summary>
    public const float DailyDecayPerDay = 2f;

    /// <summary>Lifetime se move 3 pontos por "normalização" (3x/dia).</summary>
    public const float LifetimeNormalizationPerTick = 3f;

    /// <summary>Número de normalizações por dia do jogo.</summary>
    public const int NormalizationTicksPerDay = 3;

    /// <summary>Modificadores de fúria expiram (ex: dura 12 horas).</summary>
    public const float FuryDurationHours = 12f;
}

/// <summary>
/// Pesos de atração.
/// </summary>
public static class AttractionWeights
{
    public const int TurnOnWeight = 20;
    public const int TurnOffWeight = 40; // dobro de turn-on
    public const int SameAspirationBonus = 35;
    public const int ZodiacCompatibilityMax = 30;
}

/// <summary>
/// Parâmetros do sistema de interesses / tópicos de conversa (The Sims 2).
/// </summary>
public static class InterestWeights
{
    /// <summary>Nível de interesse "neutro" (0..10): nem entedia nem empolga.</summary>
    public const float NeutralInterest = 5f;

    /// <summary>
    /// Quanto cada ponto de interesse médio (acima/abaixo do neutro) soma ao
    /// ganho de Daily de uma conversa. Média 10/10 → +3; média 0/0 → -3.
    /// </summary>
    public const float ConversationBonusPerPoint = 0.6f;
}

/// <summary>
/// Parâmetros do sistema de sentimentos (The Sims 4).
/// </summary>
public static class SentimentDefaults
{
    /// <summary>Quantos sentimentos um relacionamento mantém; o mais fraco sai.</summary>
    public const int MaxSentiments = 4;

    /// <summary>Teto de intensidade acumulada por reforço.</summary>
    public const float MaxIntensity = 5f;
}
