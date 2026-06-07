namespace EconomySystem.Core;

/// <summary>
/// Contexto passado ao avaliar uma atividade de renda: a habilidade relevante
/// do personagem (0..10, estilo The Sims 2) e um sorteio já resolvido.
/// </summary>
public readonly record struct IncomeContext(int SkillLevel, double Roll);

/// <summary>
/// Resultado de uma atividade de renda: o template de transação a creditar.
/// O resolver clona o template antes de aplicar (como o <c>InteractionEffect</c>
/// carrega um <c>RelationshipModifier</c> clonado pelo resolver).
/// </summary>
public sealed record IncomeOutcome(MoneyTransaction Template);

/// <summary>
/// Definição declarativa de uma forma de ganhar dinheiro por habilidade
/// (vender pintura, colheita, artesanato). Espelha <c>InteractionDefinition</c>:
/// <see cref="Available"/> (falha rápido) + <see cref="Succeeds"/> (chance) +
/// resultados de sucesso/fracasso.
/// </summary>
public sealed class IncomeActivityDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }

    /// <summary>Pré-condição (ex.: habilidade mínima). Falha rápido se falsa.</summary>
    public required Func<IncomeContext, bool> Available { get; init; }

    /// <summary>Deu certo? Pode usar habilidade e/ou sorteio.</summary>
    public required Func<IncomeContext, bool> Succeeds { get; init; }

    /// <summary>Ganho se bem-sucedida.</summary>
    public required IncomeOutcome OnSuccess { get; init; }

    /// <summary>Ganho (ou nada) se malsucedida.</summary>
    public required IncomeOutcome OnFailure { get; init; }
}
