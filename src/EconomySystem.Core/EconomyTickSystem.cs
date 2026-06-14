using EconomySystem.Core.Businesses;
using EconomySystem.Core.Market;

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
    private readonly BusinessResolver _business;

    public EconomyTickSystem(BillsSystem bills, CareerResolver? careers = null, BusinessResolver? business = null)
    {
        _bills = bills ?? throw new ArgumentNullException(nameof(bills));
        _careers = careers ?? new CareerResolver();
        _business = business ?? new BusinessResolver();
    }

    /// <summary>
    /// Chamado 1x por dia do jogo para um domicílio: paga salários, deprecia os
    /// bens, entrega contas nos dias de cobrança (terça/quinta), avança a
    /// tolerância do repo-man e debita assinaturas vencidas.
    /// </summary>
    public void DailyTick(
        Household household, DayOfWeek day, int? gameDay = null, decimal incomeFactor = 1m)
    {
        ArgumentNullException.ThrowIfNull(household);

        // 1. Salários de carreira (v2): renda diária dos moradores empregados,
        //    reajustada pela inflação/eventos quando dirigido pelo mercado (v5).
        foreach (var state in household.Careers.Values)
            _careers.PayDailyWage(household.Id, household.Funds, state, incomeFactor);

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

        // 6. Folha de pagamento dos negócios (v3, Open for Business).
        foreach (var biz in household.Businesses)
            _business.PayEmployees(household.Id, household.Funds, biz);
    }

    /// <summary>Conveniência: roda o DailyTick para vários domicílios.</summary>
    public void DailyTick(
        IEnumerable<Household> households, DayOfWeek day, int? gameDay = null, decimal incomeFactor = 1m)
    {
        ArgumentNullException.ThrowIfNull(households);
        foreach (var h in households)
            DailyTick(h, day, gameDay, incomeFactor);
    }

    /// <summary>
    /// Variante v4 dirigida pelo mercado: avança o calendário e a inflação e
    /// usa o próprio calendário como produtor do dia da semana e do
    /// <c>gameDay</c> (em vez de inteiros fornecidos pelo chamador). Em v5
    /// também indexa a renda do dia pelo fator de reajuste do mercado
    /// (inflação acumulada × eventos econômicos ativos).
    /// </summary>
    public void DailyTick(IEnumerable<Household> households, CurrencyMarket market)
    {
        ArgumentNullException.ThrowIfNull(market);
        market.AdvanceDay();
        DailyTick(
            households,
            market.Calendar.DayOfWeek,
            market.Calendar.CurrentDay,
            market.IncomeAdjustmentFactor());
    }
}
