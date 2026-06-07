namespace EconomySystem.Core;

/// <summary>
/// Calcula e cobra as contas do domicílio (estilo The Sims 2: proporcional ao
/// valor dos bens, com desconto por filho; repo-man se ficarem sem pagar).
/// É um "sistema" especializado, no espírito do <c>InteractionResolver</c>.
/// </summary>
public sealed class BillsSystem
{
    /// <summary>Disparado quando uma conta é entregue (paga ou não).</summary>
    public event BillEventHandler? BillDelivered;

    /// <summary>Disparado quando a tolerância se esgota e o repo-man é acionado.</summary>
    public event Action<string>? RepoManDispatched;

    /// <summary>
    /// Valor da conta: valor faturável dos bens × taxa, menos 10% por filho.
    /// </summary>
    public int ComputeBill(Household household)
    {
        ArgumentNullException.ThrowIfNull(household);

        int baseBill = household.Inventory.BillableObjectValue * EconomyPhysics.BillRatePercent / 100;
        int discountPercent = Math.Min(90, household.ChildCount * EconomyPhysics.ChildBillDiscountPercent);
        return baseBill * (100 - discountPercent) / 100;
    }

    /// <summary>
    /// Entrega a conta e tenta debitá-la. Se faltar saldo, inicia/avança a
    /// tolerância; ao esgotá-la, aciona o repo-man. Retorna true se foi paga.
    /// </summary>
    public bool DeliverAndCharge(Household household, int? gameDay = null)
    {
        ArgumentNullException.ThrowIfNull(household);

        int amount = ComputeBill(household);
        if (amount <= 0)
        {
            BillDelivered?.Invoke(household.Id, 0, true);
            return true;
        }

        var tx = new MoneyTransaction
        {
            Reason = "Contas do domicílio",
            Amount = -amount,
            Kind = TransactionKind.Bill,
            GameDay = gameDay,
        };

        bool paid = household.Funds.TryWithdraw(tx);
        BillDelivered?.Invoke(household.Id, amount, paid);

        if (paid)
        {
            household.RepoGraceDaysRemaining = 0;
        }
        else if (household.RepoGraceDaysRemaining <= 0)
        {
            household.RepoGraceDaysRemaining = EconomyThresholds.RepoManGraceDays;
        }

        return paid;
    }

    /// <summary>
    /// Avança um dia de tolerância de contas não pagas; aciona o repo-man ao
    /// chegar a zero. O <c>EconomyTickSystem</c> chama isto no DailyTick.
    /// </summary>
    public void AdvanceGrace(Household household)
    {
        ArgumentNullException.ThrowIfNull(household);
        if (household.RepoGraceDaysRemaining <= 0)
            return;

        household.RepoGraceDaysRemaining--;
        if (household.RepoGraceDaysRemaining == 0)
            RepoManDispatched?.Invoke(household.Id);
    }
}
