namespace RelationshipSystem.Core;

/// <summary>
/// Efeito de uma interação sobre o relacionamento.
/// </summary>
public sealed record InteractionEffect(
    float DailyDelta,
    float LifetimeDelta,
    RelationshipModifier? ResultingModifier = null, // opcional
    Sentiment? ResultingSentiment = null            // opcional (estilo TS4)
);

/// <summary>
/// Definição declarativa de uma interação social.
/// </summary>
public sealed class InteractionDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required bool IsRomantic { get; init; }

    /// <summary>
    /// Pré-condição: pode ser iniciada? (falha rápido)
    /// Exemplo: <c>rel => rel.EffectiveDaily >= 20 &amp;&amp; !rel.IsFurious</c>
    /// </summary>
    public required Func<Relationship, bool> Available { get; init; }

    /// <summary>
    /// Regra de aceitação: vai dar certo?
    /// Pode usar Random, atração, mood, etc.
    /// </summary>
    public required Func<Relationship, bool> Accepted { get; init; }

    /// <summary>Efeitos se aceita.</summary>
    public required InteractionEffect OnAccept { get; init; }

    /// <summary>Efeitos se rejeita.</summary>
    public required InteractionEffect OnReject { get; init; }
}
