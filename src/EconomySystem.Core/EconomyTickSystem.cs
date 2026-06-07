namespace EconomySystem.Core;

/// <summary>
/// Passagem de tempo da economia. Espelha o <c>RelationshipDecaySystem</c>:
/// um tick diário que percorre os domicílios. Mantém o loop fechado
/// (ganhar → guardar → gastar → o tempo passa).
/// </summary>
public sealed class EconomyTickSystem
{
    private readonly BillsSystem _bills;
    private readonly CareerResolver _careers;

    public EconomyTickSystem(BillsSystem bills, CareerResolver? careers = null)
    {
        _bills = bills ?? throw new ArgumentNullException(nameof(bills));
        _careers = careers ?? new CareerResolver();
    }

    /// <summary>
    /// Chamado 1x por dia do jogo para um domicílio: paga salários, deprecia os
    /// bens, entrega contas nos dias de cobrança (terça/quinta), avança a
    /// tolerância do repo-man e debita assinaturas vencidas.
    /// </summary>
    public void DailyTick(Household household, DayOfWeek day, int? gameDay = null)
    {
        ArgumentNullException.ThrowIfNull(household);

        // 1. Salários de carreira (v2): renda diária dos moradores empregados.
        foreach (var state in household.Careers.Values)
            _careers.PayDailyWage(household.Id, household.Funds, state);

        // 2. Bens depreciam.
        foreach (var obj in household.Inventory.All)
            obj.DepreciateOneDay(EconomyPhysics.DepreciationPercentPerDay);

        // 3. Avança tolerância de contas atrasadas (pode acionar o repo-man).
        _bills.AdvanceGrace(household);

        // 4. Em dia de cobrança, entrega e tenta debitar a conta.
        if (EconomyPhysics.BillDeliveryDays.Contains(day))
            _bills.DeliverAndCharge(household, gameDay);

        // 5. Assinaturas digitais vencidas (v2): micro-contas recorrentes.
        foreach (var sub in household.Subscriptions)
            if (sub.AdvanceDay())
                household.Funds.TryWithdraw(sub.ToDebit());

        // TODO (v3): folha de pagamento dos funcionários do negócio.
    }

    /// <summary>Conveniência: roda o DailyTick para vários domicílios.</summary>
    public void DailyTick(IEnumerable<Household> households, DayOfWeek day, int? gameDay = null)
    {
        ArgumentNullException.ThrowIfNull(households);
        foreach (var h in households)
            DailyTick(h, day, gameDay);
    }
}
