namespace RelationshipSystem.Core.Interactions;

/// <summary>
/// Catálogo de interações sociais prontas.
/// </summary>
public static class InteractionLibrary
{
    // Random compartilhado: evita o anti-padrão de instanciar Random em loop
    // (instâncias criadas em rápida sucessão compartilhariam a mesma seed).
    private static readonly Random Rng = Random.Shared;

    public static readonly InteractionDefinition Talk = new()
    {
        Id = "Talk",
        DisplayName = "Conversar",
        IsRomantic = false,
        Available = rel => rel.EffectiveDaily > RelationshipThresholds.Enemy,
        Accepted = _ => true, // sempre aceita
        OnAccept = new InteractionEffect(DailyDelta: 3f, LifetimeDelta: 1f),
        OnReject = new InteractionEffect(DailyDelta: 0f, LifetimeDelta: 0f),
    };

    public static readonly InteractionDefinition Compliment = new()
    {
        Id = "Compliment",
        DisplayName = "Elogiar",
        IsRomantic = false,
        Available = rel => rel.EffectiveDaily >= 20 && !rel.IsFurious,
        Accepted = _ => Rng.Next(100) > 20, // 80% aceita
        OnAccept = new InteractionEffect(DailyDelta: 5f, LifetimeDelta: 2f),
        OnReject = new InteractionEffect(DailyDelta: -2f, LifetimeDelta: 0f),
    };

    public static readonly InteractionDefinition Flirt = new()
    {
        Id = "Flirt",
        DisplayName = "Flerte",
        IsRomantic = true,
        Available = rel => rel.EffectiveDaily >= 20 && !rel.IsFurious,
        Accepted = rel =>
        {
            // Atração funciona como facilitador.
            float threshold = 30f - (rel.AttractionScore / 10f);
            return rel.EffectiveDaily / 2f + rel.AttractionScore / 10f > threshold;
        },
        OnAccept = new InteractionEffect(DailyDelta: 8f, LifetimeDelta: 3f),
        OnReject = new InteractionEffect(DailyDelta: -5f, LifetimeDelta: 0f),
    };

    public static readonly InteractionDefinition GiveGift = new()
    {
        Id = "GiveGift",
        DisplayName = "Dar Presente",
        IsRomantic = false,
        Available = rel => rel.EffectiveDaily > -30,
        Accepted = _ => true,
        OnAccept = new InteractionEffect(
            DailyDelta: 10f,
            LifetimeDelta: 5f, // presentes afetam lifetime!
            ResultingModifier: new RelationshipModifier
            {
                Name = "Recebeu um presente",
                Value = 10f,
                RemainingHours = 48f
            },
            ResultingSentiment: new Sentiment
            {
                Type = SentimentType.Adoring,
                Intensity = 1f,
                IsLongTerm = false,
                RemainingHours = 48f
            }
        ),
        OnReject = new InteractionEffect(DailyDelta: 0f, LifetimeDelta: 0f),
    };

    public static readonly InteractionDefinition Insult = new()
    {
        Id = "Insult",
        DisplayName = "Insultar",
        IsRomantic = false,
        Available = rel => rel.EffectiveDaily > -80, // só se não for ódio total
        Accepted = _ => true,
        OnAccept = new InteractionEffect(
            DailyDelta: -15f,
            LifetimeDelta: -5f,
            ResultingModifier: new RelationshipModifier
            {
                Name = Relationship.FuryModifierName,
                Value = -20f,
                RemainingHours = RelationshipPhysics.FuryDurationHours
            },
            ResultingSentiment: new Sentiment
            {
                Type = SentimentType.Resentful,
                Intensity = 1f,
                IsLongTerm = false,
                RemainingHours = 48f
            }
        ),
        OnReject = new InteractionEffect(DailyDelta: 0f, LifetimeDelta: 0f),
    };

    /// <summary>Todas as interações registradas, indexadas por Id.</summary>
    public static readonly IReadOnlyDictionary<string, InteractionDefinition> All =
        new[] { Talk, Compliment, Flirt, GiveGift, Insult }
            .ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
}
