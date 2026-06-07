namespace EconomySystem.Core;

/// <summary>
/// Assinatura digital recorrente (plano de celular, streaming) — uma micro-conta
/// que o The Sims 2 não tinha, parte da modernização da v2. Estruturalmente é um
/// template de <see cref="MoneyTransaction"/> que se re-arma a cada ciclo. O
/// <c>EconomyTickSystem</c> a debita quando vence.
/// </summary>
public sealed class Subscription
{
    private readonly string _name = string.Empty;

    public required string Name
    {
        get => _name;
        init => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Subscription name required", nameof(Name))
            : value;
    }

    /// <summary>Custo por ciclo de cobrança, em Simoleons.</summary>
    public required int Cost { get; init; }

    /// <summary>Duração do ciclo, em dias de jogo (default: uma "semana-mês").</summary>
    public int BillingPeriodDays { get; init; } = EconomyPhysics.SubscriptionBillingDays;

    /// <summary>Dias até a próxima cobrança.</summary>
    public int DaysUntilDebit { get; private set; } = EconomyPhysics.SubscriptionBillingDays;

    /// <summary>
    /// Avança um dia. Retorna true (e re-arma o ciclo) quando vence — o tick usa
    /// isso para decidir se debita.
    /// </summary>
    public bool AdvanceDay()
    {
        DaysUntilDebit--;
        if (DaysUntilDebit > 0)
            return false;

        DaysUntilDebit = BillingPeriodDays;
        return true;
    }

    /// <summary>Cria a transação de débito desta assinatura.</summary>
    public MoneyTransaction ToDebit() => new()
    {
        Reason = $"Assinatura: {Name}",
        Amount = -Cost,
        Kind = TransactionKind.Subscription,
    };
}
