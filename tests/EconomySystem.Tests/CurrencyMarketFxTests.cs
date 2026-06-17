using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class CurrencyMarketFxTests
{
    private static CurrencyMarket FxMarket(decimal volatility, int seed)
    {
        var market = new CurrencyMarket(exchangeRates: new ExchangeRateEngine(seed));
        market.Currencies.Add(new Currency
        {
            Id = "Dolar", Name = "$Dolar", Symbol = "$D",
            UnitsPerMoney = 5.2m, ExchangeRateVolatilityPercent = volatility,
        });
        return market;
    }

    [Fact]
    public void Floating_rate_makes_quotes_move_over_time()
    {
        var market = FxMarket(volatility: 10m, seed: 1);

        var seen = new HashSet<int>();
        for (int i = 0; i < 30; i++)
        {
            market.AdvanceDay();
            seen.Add(market.Quote(10_000, "Dolar").RoundedPrice);
        }

        Assert.True(seen.Count > 1, "câmbio flutuante deveria mover a cotação ao longo dos dias");
    }

    [Fact]
    public void Fixed_currency_quote_is_stable()
    {
        var market = FxMarket(volatility: 0m, seed: 1);

        for (int i = 0; i < 30; i++) market.AdvanceDay();

        Assert.Equal(52_000, market.Quote(10_000, "Dolar").RoundedPrice); // 10000 × 5.2
        Assert.Equal(5.2m, market.EffectiveRate("Dolar"));
    }

    [Fact]
    public void Effective_rate_combines_nominal_and_floating_index()
    {
        var market = FxMarket(volatility: 8m, seed: 4);
        for (int i = 0; i < 20; i++) market.AdvanceDay();

        decimal expected = 5.2m * market.ExchangeRates.RateIndex("Dolar");
        Assert.Equal(expected, market.EffectiveRate("Dolar"));
    }

    [Fact]
    public void Removing_currency_clears_its_rate_index()
    {
        var market = FxMarket(volatility: 10m, seed: 7);
        for (int i = 0; i < 10; i++) market.AdvanceDay();
        Assert.NotEqual(1m, market.ExchangeRates.RateIndex("Dolar"));

        Assert.True(market.RemoveCurrency("Dolar"));
        Assert.Equal(1m, market.ExchangeRates.RateIndex("Dolar"));
    }

    [Fact]
    public void Base_currency_is_never_affected_by_fx()
    {
        var market = FxMarket(volatility: 20m, seed: 3);
        for (int i = 0; i < 100; i++) market.AdvanceDay();

        Assert.Equal(10_000, market.Quote(10_000, MarketRules.BaseCurrencyId).RoundedPrice);
    }
}
