namespace EconomySystem.Core.Digital;

/// <summary>
/// Catálogo de rendas DIGITAIS (gig economy, marketplace online) — a
/// modernização da v2. Note que são apenas instâncias de
/// <see cref="IncomeActivityDefinition"/>: o mesmo esquema da v1 generaliza para
/// o mundo moderno sem máquinas novas. Reusa o <c>IncomeResolver</c> existente.
/// </summary>
public static class DigitalIncomeLibrary
{
    /// <summary>Bico por app (entrega/transporte). Disponível a qualquer um.</summary>
    public static readonly IncomeActivityDefinition AppGig = new()
    {
        Id = "AppGig",
        DisplayName = "Bico por app",
        Available = _ => true,
        Succeeds = ctx => ctx.Roll < 0.85, // a corrida quase sempre fecha
        OnSuccess = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Corrida por app",
            Amount = 45,
            Kind = TransactionKind.Freelance,
        }),
        OnFailure = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Corrida cancelada",
            Amount = 0,
            Kind = TransactionKind.Freelance,
        }),
    };

    /// <summary>
    /// Venda no marketplace online. Vende mais caro com mais habilidade
    /// (fotos/descrição melhores), mas há chance de não vender.
    /// </summary>
    public static readonly IncomeActivityDefinition MarketplaceSale = new()
    {
        Id = "MarketplaceSale",
        DisplayName = "Venda no marketplace",
        Available = _ => true,
        Succeeds = ctx => ctx.Roll < 0.4 + ctx.SkillLevel * 0.05,
        OnSuccess = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Venda online",
            Amount = 90,
            Kind = TransactionKind.Freelance,
        }),
        OnFailure = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Anúncio sem comprador",
            Amount = 0,
            Kind = TransactionKind.Freelance,
        }),
    };

    /// <summary>Todas as rendas digitais registradas, indexadas por Id.</summary>
    public static readonly IReadOnlyDictionary<string, IncomeActivityDefinition> All =
        new[] { AppGig, MarketplaceSale }
            .ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
}
