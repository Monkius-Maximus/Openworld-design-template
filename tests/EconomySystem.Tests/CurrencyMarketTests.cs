using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class CurrencyMarketTests
{
    [Fact]
    public void RemoveCurrency_clears_accumulated_inflation_index()
    {
        var market = new CurrencyMarket();
        market.Currencies.Add(new Currency
        {
            Id = "Dolar",
            Name = "$Dolar",
            Symbol = "$D",
            UnitsPerMoney = 5.2m,
            ProjectedAnnualInflationPercent = 10m,
            CreatedOnDay = 0,
        });

        for (int i = 0; i < MarketRules.DaysPerYear + 30; i++)
            market.AdvanceDay();
        Assert.True(market.Inflation.CurrencyIndex("Dolar") > 1m);

        Assert.True(market.RemoveCurrency("Dolar"));
        Assert.Equal(1m, market.Inflation.CurrencyIndex("Dolar"));
        Assert.False(market.RemoveCurrency("Dolar"));
    }

    [Fact]
    public void Quoting_a_removed_currency_throws()
    {
        var market = new CurrencyMarket();
        market.Currencies.Add(new Currency
        { Id = "Dolar", Name = "$Dolar", Symbol = "$D", UnitsPerMoney = 5.2m });

        market.RemoveCurrency("Dolar");

        Assert.Throws<KeyNotFoundException>(() => market.Quote(100, "Dolar"));
    }

    [Fact]
    public void Market_driven_tick_advances_calendar_and_labels_gameday()
    {
        var market = new CurrencyMarket();
        var household = new EconomySystem.Core.Household
        {
            Id = "lar",
            Funds = new EconomySystem.Core.HouseholdFunds(1_000),
        };
        var ticks = new EconomySystem.Core.EconomyTickSystem(new EconomySystem.Core.BillsSystem());

        ticks.DailyTick(new[] { household }, market);

        Assert.Equal(1, market.Calendar.CurrentDay);
        Assert.Equal(DayOfWeek.Tuesday, market.Calendar.DayOfWeek);
    }
}
