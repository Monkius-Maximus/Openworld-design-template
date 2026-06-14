using EconomySystem.Core;
using EconomySystem.Core.Careers;
using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class IndexedIncomeTests
{
    private static CareerState FreshBusiness() =>
        new() { CharacterId = "alice", Career = CareerLibrary.Business }; // nível 1 = 140/dia

    [Fact]
    public void Income_factor_scales_the_paid_wage()
    {
        var careers = new CareerResolver();
        var funds = new HouseholdFunds(0);

        careers.PayDailyWage("lar", funds, FreshBusiness(), incomeFactor: 1.5m);

        Assert.Equal(210, funds.Balance); // 140 × 1.5
    }

    [Fact]
    public void Factor_of_one_preserves_classic_wage()
    {
        var careers = new CareerResolver();
        var funds = new HouseholdFunds(0);

        careers.PayDailyWage("lar", funds, FreshBusiness()); // default 1m

        Assert.Equal(140, funds.Balance);
    }

    [Fact]
    public void Negative_factor_throws()
    {
        var careers = new CareerResolver();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            careers.PayDailyWage("lar", new HouseholdFunds(0), FreshBusiness(), incomeFactor: -1m));
    }

    [Fact]
    public void Market_driven_tick_indexes_wages_to_accumulated_inflation()
    {
        var careers = new CareerResolver();
        var tick = new EconomyTickSystem(new BillsSystem(), careers);

        var household = new Household { Id = "lar", Funds = new HouseholdFunds(0) };
        household.Careers["alice"] = FreshBusiness();

        // Mercado com forte inflação global; acumula quase um ano de deriva.
        var market = new CurrencyMarket();
        market.Inflation.GlobalAnnualPercent = 100m;
        for (int i = 0; i < MarketRules.DaysPerYear - 1; i++)
            market.AdvanceDay();

        int paidWage = 0;
        careers.WagePaid += (_, txn) => paidWage = txn.Amount;

        // O tick dirigido pelo mercado avança o último dia e paga já indexado.
        tick.DailyTick(new[] { household }, market);

        // ≈100% de inflação no ano → salário reajustado para perto do dobro.
        Assert.True(paidWage > 140, $"esperava salário indexado acima de 140, veio {paidWage}");
        Assert.InRange(paidWage, 270, 285); // 140 × ~2.0
    }

    [Fact]
    public void Recession_event_cuts_real_income_below_classic_wage()
    {
        var careers = new CareerResolver();
        var tick = new EconomyTickSystem(new BillsSystem(), careers);

        var household = new Household { Id = "lar", Funds = new HouseholdFunds(0) };
        household.Careers["alice"] = FreshBusiness();

        // Sem inflação base, mas uma recessão (renda ×0.85) começando amanhã.
        var market = new CurrencyMarket();
        market.Events.Schedule(EconomicEventLibrary.Recession(startDay: 1));

        int paidWage = 0;
        careers.WagePaid += (_, txn) => paidWage = txn.Amount;

        tick.DailyTick(new[] { household }, market); // dia 1: recessão ativa

        Assert.Equal(119, paidWage); // 140 × 0.85
    }
}
