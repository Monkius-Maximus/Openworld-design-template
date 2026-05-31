namespace RelationshipSystem.Core;

/// <summary>
/// Calcula atração entre dois personagens (TS2 + melhorias).
/// A atração é assimétrica: <c>Calculate(a, b)</c> pode diferir de
/// <c>Calculate(b, a)</c>.
/// </summary>
public static class AttractionCalculator
{
    /// <summary>
    /// Calcula atração assimétrica: quanto A é atraído por B.
    /// </summary>
    public static int Calculate(CharacterTraits a, CharacterTraits b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        int score = 0;

        // Turn-ons
        foreach (var turnOn in a.TurnOns)
        {
            if (b.Tags.Contains(turnOn, StringComparer.OrdinalIgnoreCase))
                score += AttractionWeights.TurnOnWeight;
        }

        // Turn-off
        if (b.Tags.Contains(a.TurnOff, StringComparer.OrdinalIgnoreCase))
            score -= AttractionWeights.TurnOffWeight;

        // Aspiração
        if (a.Aspiration == b.Aspiration)
            score += AttractionWeights.SameAspirationBonus;

        // Zodíaco
        score += GetZodiacModifier(a.Zodiac, b.Zodiac);

        // Personalidade (similaridade)
        score += GetPersonalitySimilarity(a.Personality, b.Personality);

        return score;
    }

    /// <summary>
    /// Chemistry = média das duas direções (mutual, TS2).
    /// </summary>
    public static int Chemistry(CharacterTraits a, CharacterTraits b) =>
        (Calculate(a, b) + Calculate(b, a)) / 2;

    private static int GetPersonalitySimilarity(Personality x, Personality y)
    {
        int diff = Math.Abs(x.Neat - y.Neat)
                 + Math.Abs(x.Outgoing - y.Outgoing)
                 + Math.Abs(x.Active - y.Active)
                 + Math.Abs(x.Playful - y.Playful)
                 + Math.Abs(x.Nice - y.Nice);

        // Mais parecidos = maior score.
        return 25 - diff;
    }

    private static int GetZodiacModifier(Zodiac a, Zodiac b) =>
        ZodiacCompatibilityTable.Get(a, b);
}
