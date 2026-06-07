namespace EconomySystem.Core.Economy;

/// <summary>
/// Catálogo de atividades de renda por habilidade prontas (estilo The Sims 2:
/// pintar, vender colheita, artesanato). Espelha o <c>InteractionLibrary</c>:
/// definições estáticas indexadas por Id, case-insensitive.
/// </summary>
public static class IncomeActivityLibrary
{
    /// <summary>
    /// Vender uma pintura. Requer Criatividade &gt;= 2; o valor cresce com a
    /// habilidade. Chance de sucesso também escala com a habilidade.
    /// </summary>
    public static readonly IncomeActivityDefinition SellPainting = new()
    {
        Id = "SellPainting",
        DisplayName = "Vender pintura",
        Available = ctx => ctx.SkillLevel >= 2,
        Succeeds = ctx => ctx.Roll < 0.5 + ctx.SkillLevel * 0.05, // 55%..100%
        OnSuccess = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Venda de pintura",
            Amount = 120,
            Kind = TransactionKind.Freelance,
        }),
        OnFailure = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Pintura sem comprador",
            Amount = 0, // sem venda: nada creditado (resolver só deposita > 0)
            Kind = TransactionKind.Freelance,
        }),
    };

    /// <summary>Vender colheita do jardim. Requer Jardinagem (mapeada à habilidade) &gt;= 1.</summary>
    public static readonly IncomeActivityDefinition SellGardenProduce = new()
    {
        Id = "SellGardenProduce",
        DisplayName = "Vender colheita",
        Available = ctx => ctx.SkillLevel >= 1,
        Succeeds = _ => true, // colheita sempre vende; quantidade varia
        OnSuccess = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Venda de colheita",
            Amount = 60,
            Kind = TransactionKind.Freelance,
        }),
        OnFailure = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Colheita estragada",
            Amount = 0, // sem venda: nada creditado
            Kind = TransactionKind.Freelance,
        }),
    };

    /// <summary>Artesanato (ex.: robôs/brinquedos). Requer Mecânica &gt;= 4.</summary>
    public static readonly IncomeActivityDefinition Craft = new()
    {
        Id = "Craft",
        DisplayName = "Vender artesanato",
        Available = ctx => ctx.SkillLevel >= 4,
        Succeeds = ctx => ctx.Roll < 0.4 + ctx.SkillLevel * 0.06,
        OnSuccess = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Venda de artesanato",
            Amount = 250,
            Kind = TransactionKind.Freelance,
        }),
        OnFailure = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Peça com defeito",
            Amount = 0, // sem venda: nada creditado
            Kind = TransactionKind.Freelance,
        }),
    };

    /// <summary>Todas as atividades registradas, indexadas por Id.</summary>
    public static readonly IReadOnlyDictionary<string, IncomeActivityDefinition> All =
        new[] { SellPainting, SellGardenProduce, Craft }
            .ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
}
