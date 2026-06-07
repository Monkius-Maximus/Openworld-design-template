namespace RelationshipSystem.Core;

/// <summary>
/// Tópicos de conversa comuns (The Sims 2). São apenas strings — qualquer
/// tópico livre é aceito; estas constantes existem só para descoberta/consistência.
/// </summary>
public static class InterestTopics
{
    public const string Sports = "Sports";
    public const string Politics = "Politics";
    public const string Crime = "Crime";
    public const string Money = "Money";
    public const string Culture = "Culture";
    public const string Food = "Food";
    public const string Travel = "Travel";
    public const string Weather = "Weather";
    public const string Health = "Health";
    public const string Paranormal = "Paranormal";
}

/// <summary>
/// Calcula o quanto um tópico de conversa "engata" entre dois personagens,
/// a partir dos seus níveis de interesse (0..10). A lógica é simétrica:
/// uma boa conversa exige interesse mútuo.
/// </summary>
public static class InterestCalculator
{
    /// <summary>
    /// Interesse compartilhado em um tópico, em torno do neutro (5).
    /// Retorna [-5, +5]: ambos a 10 → +5; ambos a 0 → -5; média neutra → 0.
    /// Tópico ausente em um personagem conta como interesse 0 (entediante).
    /// </summary>
    public static float SharedInterest(CharacterTraits a, CharacterTraits b, string topic)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic required", nameof(topic));

        float average = (Level(a, topic) + Level(b, topic)) / 2f;
        return average - InterestWeights.NeutralInterest;
    }

    /// <summary>
    /// Bônus (ou penalidade) de Daily aplicado a uma conversa sobre o tópico.
    /// Conversar sobre algo que ambos amam acelera o relacionamento; sobre algo
    /// que entedia os dois, esfria.
    /// </summary>
    public static float ConversationModifier(CharacterTraits a, CharacterTraits b, string topic) =>
        SharedInterest(a, b, topic) * InterestWeights.ConversationBonusPerPoint;

    private static int Level(CharacterTraits c, string topic) =>
        c.Interests.TryGetValue(topic, out int level) ? level : 0;
}
