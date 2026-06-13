using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class InflationEngineTests
{
    private static CurrencyMarket NewMarket() => new();

    private static void AdvanceDays(CurrencyMarket market, int days)
    {
        for (int i = 0; i < days; i++)
            market.AdvanceDay();
    }

    [Fact]
    public void Global_inflation_compounds_to_annual_rate_after_one_year()
    {
        var market = NewMarket();
        market.Inflation.GlobalAnnualPercent = 10m;

        AdvanceDays(market, MarketRules.DaysPerYear);

        Assert.True(Math.Abs(market.Inflation.GlobalIndex - 1.10m) < 0.001m,
            $"GlobalIndex era {market.Inflation.GlobalIndex}, esperado ≈1.10");
    }

    [Fact]
    public void Zero_rate_keeps_index_exactly_one()
    {
        var market = NewMarket();
        AdvanceDays(market, MarketRules.DaysPerYear);
        Assert.Equal(1m, market.Inflation.GlobalIndex);
    }

    [Fact]
    public void Product_inflation_affects_only_that_product()
    {
        var market = NewMarket();
        market.Inflation.SetProductInflation("carro", 10m);

        AdvanceDays(market, MarketRules.DaysPerYear);

        Assert.True(Math.Abs(market.Inflation.ProductIndex("carro") - 1.10m) < 0.001m);
        Assert.Equal(1m, market.Inflation.ProductIndex("sofa"));
        Assert.Equal(1m, market.Inflation.GlobalIndex);
    }

    [Fact]
    public void Product_inflation_lookup_is_case_insensitive()
    {
        var market = NewMarket();
        market.Inflation.SetProductInflation("Carro", 10m);
        AdvanceDays(market, 10);
        Assert.Equal(market.Inflation.ProductIndex("Carro"), market.Inflation.ProductIndex("CARRO"));
    }

    [Fact]
    public void Currency_inflation_is_dormant_until_one_simulated_year_after_creation()
    {
        var market = NewMarket();
        market.Currencies.Add(new Currency
        {
            Id = "Dolar",
            Name = "$Dolar",
            Symbol = "$D",
            UnitsPerMoney = 5.2m,
            ProjectedAnnualInflationPercent = 10m,
            CreatedOnDay = market.Calendar.CurrentDay,
        });

        // Até a véspera da ativação (dia 363), o índice fica exatamente em 1.
        AdvanceDays(market, MarketRules.DaysPerYear - 1);
        Assert.Equal(1m, market.Inflation.CurrencyIndex("Dolar"));

        // No dia da ativação começa a compor.
        market.AdvanceDay();
        Assert.True(market.Inflation.CurrencyIndex("Dolar") > 1m);

        // Um ano inteiro de inflação ativa ≈ taxa anual cheia.
        AdvanceDays(market, MarketRules.DaysPerYear - 1);
        Assert.True(Math.Abs(market.Inflation.CurrencyIndex("Dolar") - 1.10m) < 0.001m,
            $"CurrencyIndex era {market.Inflation.CurrencyIndex("Dolar")}, esperado ≈1.10");
    }

    [Fact]
    public void Base_currency_supports_local_inflation_via_same_rule()
    {
        var calendar = new SimulationCalendar();
        var registry = new CurrencyRegistry(new Currency
        {
            Id = MarketRules.BaseCurrencyId,
            Name = MarketRules.BaseCurrencyName,
            Symbol = MarketRules.BaseCurrencySymbol,
            UnitsPerMoney = 1m,
            ProjectedAnnualInflationPercent = 10m,
            IsBase = true,
        });
        var market = new CurrencyMarket(calendar, registry);

        AdvanceDays(market, MarketRules.DaysPerYear - 1);
        Assert.Equal(1m, market.Inflation.CurrencyIndex(MarketRules.BaseCurrencyId));

        market.AdvanceDay();
        Assert.True(market.Inflation.CurrencyIndex(MarketRules.BaseCurrencyId) > 1m);
    }

    [Fact]
    public void RemoveProductInflation_freezes_and_forgets_index()
    {
        var market = NewMarket();
        market.Inflation.SetProductInflation("carro", 50m);
        AdvanceDays(market, 30);
        Assert.True(market.Inflation.ProductIndex("carro") > 1m);

        Assert.True(market.Inflation.RemoveProductInflation("carro"));
        Assert.Equal(1m, market.Inflation.ProductIndex("carro"));
        Assert.False(market.Inflation.RemoveProductInflation("carro"));
    }

    [Fact]
    public void Rates_outside_bounds_throw()
    {
        var engine = new InflationEngine();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.GlobalAnnualPercent = MarketRules.MaxAnnualInflationPercent + 1);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.SetProductInflation("carro", MarketRules.MinAnnualInflationPercent - 1));
    }
}
