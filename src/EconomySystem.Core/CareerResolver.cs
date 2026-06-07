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

    /// <summary>Disparado quando um chance card é resolvido.</summary>
    public event ChanceCardHandler? ChanceCardResolved;

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

    /// <summary>
    /// Resolve um chance card aplicando a opção escolhida: ajusta o caixa e a
    /// carreira (promoção/rebaixamento). A promoção por card é uma recompensa —
    /// ignora os requisitos normais (mas não passa do topo).
    /// </summary>
    public void ResolveChanceCard(string householdId, HouseholdFunds funds, CareerState state,
        ChanceCard card, bool chooseA)
    {
        if (string.IsNullOrWhiteSpace(householdId))
            throw new ArgumentException("householdId required", nameof(householdId));
        ArgumentNullException.ThrowIfNull(funds);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(card);

        var outcome = chooseA ? card.OptionA : card.OptionB;

        if (outcome.FundsDelta > 0)
        {
            funds.Deposit(new MoneyTransaction
            {
                Reason = $"Chance card: {card.Id}",
                Amount = outcome.FundsDelta,
                Kind = TransactionKind.ChanceCard,
            });
        }
        else if (outcome.FundsDelta < 0)
        {
            funds.TryWithdraw(new MoneyTransaction
            {
                Reason = $"Chance card: {card.Id}",
                Amount = outcome.FundsDelta,
                Kind = TransactionKind.ChanceCard,
            });
        }

        switch (outcome.Effect)
        {
            case CareerEffect.Promote when !state.IsAtTop:
                state.Promote();
                break;
            case CareerEffect.Demote:
                state.Demote();
                break;
        }

        ChanceCardResolved?.Invoke(state.CharacterId, outcome);
    }
}
