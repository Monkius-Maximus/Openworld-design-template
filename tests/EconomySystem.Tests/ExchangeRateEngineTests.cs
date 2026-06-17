using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class ExchangeRateEngineTests
{
    private static CurrencyRegistry RegistryWith(decimal volatility)
    {
        var registry = new CurrencyRegistry();
        registry.Add(new Currency
        {
            Id = "Dolar", Name = "$Dolar", Symbol = "$D",
            UnitsPerMoney = 5.2m, ExchangeRateVolatilityPercent = volatility,
        });
        return registry;
    }

    [Fact]
    public void Same_seed_reproduces_the_same_walk()
    {
        var a = new ExchangeRateEngine(seed: 5);
        var b = new ExchangeRateEngine(seed: 5);
        var ra = RegistryWith(10m);
        var rb = RegistryWith(10m);

        for (int i = 0; i < 30; i++) { a.AdvanceDay(ra); b.AdvanceDay(rb); }

        Assert.Equal(a.RateIndex("Dolar"), b.RateIndex("Dolar"));
        Assert.NotEqual(1m, a.RateIndex("Dolar")); // de fato caminhou
    }

    [Fact]
    public void Index_stays_within_the_clamp_band()
    {
        var engine = new ExchangeRateEngine(seed: 99);
        var registry = RegistryWith(MarketRules.MaxExchangeRateVolatilityPercent);

        for (int i = 0; i < 2000; i++)
        {
            engine.AdvanceDay(registry);
            Assert.InRange(engine.RateIndex("Dolar"),
                MarketRules.MinRateIndex, MarketRules.MaxRateIndex);
        }
    }

    [Fact]
    public void Zero_volatility_and_base_stay_inert()
    {
        var engine = new ExchangeRateEngine(seed: 1);
        var registry = RegistryWith(0m);

        for (int i = 0; i < 50; i++) engine.AdvanceDay(registry);

        Assert.Equal(1m, engine.RateIndex("Dolar"));
        Assert.Equal(1m, engine.RateIndex(MarketRules.BaseCurrencyId));
    }

    [Fact]
    public void Clearing_a_currency_resets_its_index()
    {
        var engine = new ExchangeRateEngine(seed: 2);
        var registry = RegistryWith(10m);
        for (int i = 0; i < 10; i++) engine.AdvanceDay(registry);

        Assert.NotEqual(1m, engine.RateIndex("Dolar"));
        engine.ClearRateIndex("Dolar");
        Assert.Equal(1m, engine.RateIndex("Dolar"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(26)]
    public void Currency_rejects_out_of_range_volatility(decimal volatility)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Currency
        {
            Id = "X", Name = "X", Symbol = "X",
            UnitsPerMoney = 1m, ExchangeRateVolatilityPercent = volatility,
        });
    }
}
