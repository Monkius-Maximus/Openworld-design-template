namespace EconomySystem.Core;

/// <summary>
/// Opera carreiras: paga salário e processa promoções. Espelha o
/// <c>InteractionResolver</c> (aplica + emite eventos). A contagem de amigos
/// (gate de promoção do The Sims 2) é injetada via <see cref="CareerContext"/>,
/// tipicamente preenchida pelo <c>RelationshipEconomyBridge</c>.
/// </summary>
public sealed class CareerResolver
{
    /// <summary>Disparado quando um personagem é promovido.</summary>
    public event PromotionEventHandler? Promoted;

    /// <summary>Disparado a cada salário pago.</summary>
    public event MoneyEventHandler? WagePaid;

    /// <summary>
    /// Tenta promover. Retorna true se subiu de nível (e emite <see cref="Promoted"/>).
    /// </summary>
    public bool TryPromote(CareerState state, CareerContext ctx)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.EligibleForPromotion(ctx))
            return false;

        state.Promote();
        var level = state.Career.LevelAt(state.CurrentLevel)!;
        Promoted?.Invoke(state.CharacterId, level);
        return true;
    }

    /// <summary>Paga o salário diário do nível atual ao caixa do domicílio.</summary>
    public void PayDailyWage(string householdId, HouseholdFunds funds, CareerState state)
    {
        if (string.IsNullOrWhiteSpace(householdId))
            throw new ArgumentException("householdId required", nameof(householdId));
        ArgumentNullException.ThrowIfNull(funds);
        ArgumentNullException.ThrowIfNull(state);

        int wage = state.DailyWage;
        if (wage <= 0)
            return;

        var tx = new MoneyTransaction
        {
            Reason = $"Salário: {state.Career.DisplayName} ({state.Title})",
            Amount = wage,
            Kind = TransactionKind.Wage,
        };
        funds.Deposit(tx);
        WagePaid?.Invoke(householdId, tx);
    }
}
