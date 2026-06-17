using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class MarketStateSerializerTests
{
    private static CurrencyMarket BuildSampleMarket()
    {
        var market = new CurrencyMarket();
        market.Inflation.GlobalAnnualPercent = 5m;
        market.Inflation.SetProductInflation("carro", 8m);
        market.Currencies.Add(new Currency
        {
            Id = "Dolar",
            Name = "$Dolar",
            Symbol = "$D",
            UnitsPerMoney = 5.2m,
            ProjectedAnnualInflationPercent = 4.5m,
            CreatedOnDay = 0,
        });

        // Atravessa a ativação da inflação do $Dolar para acumular deriva real
        // nas três famílias de índice.
        for (int i = 0; i < MarketRules.DaysPerYear + 48; i++)
            market.AdvanceDay();

        return market;
    }

    [Fact]
    public void Round_trip_preserves_day_rates_and_accumulated_indices()
    {
        var original = BuildSampleMarket();

        var restored = MarketStateSerializer.FromJson(MarketStateSerializer.ToJson(original));

        Assert.Equal(original.Calendar.CurrentDay, restored.Calendar.CurrentDay);
        Assert.Equal(original.Inflation.GlobalAnnualPercent, restored.Inflation.GlobalAnnualPercent);
        Assert.Equal(original.Inflation.GlobalIndex, restored.Inflation.GlobalIndex);
        Assert.Equal(original.Inflation.ProductIndex("carro"), restored.Inflation.ProductIndex("carro"));
        Assert.Equal(original.Inflation.CurrencyIndex("Dolar"), restored.Inflation.CurrencyIndex("Dolar"));

        var dolar = restored.Currencies.Get("Dolar");
        Assert.Equal("$Dolar", dolar.Name);
        Assert.Equal("$D", dolar.Symbol);
        Assert.Equal(5.2m, dolar.UnitsPerMoney);
        Assert.Equal(4.5m, dolar.ProjectedAnnualInflationPercent);
        Assert.True(restored.Currencies.Base.IsBase);
    }

    [Fact]
    public void Advancing_after_load_continues_compounding_from_saved_index()
    {
        var original = BuildSampleMarket();
        var restored = MarketStateSerializer.FromJson(MarketStateSerializer.ToJson(original));

        original.AdvanceDay();
        restored.AdvanceDay();

        Assert.Equal(original.Inflation.GlobalIndex, restored.Inflation.GlobalIndex);
        Assert.Equal(original.Inflation.ProductIndex("carro"), restored.Inflation.ProductIndex("carro"));
        Assert.Equal(original.Inflation.CurrencyIndex("Dolar"), restored.Inflation.CurrencyIndex("Dolar"));
        Assert.Equal(
            original.Quote(MarketRules.SamplePreviewPriceMoney, "Dolar", "carro").RoundedPrice,
            restored.Quote(MarketRules.SamplePreviewPriceMoney, "Dolar", "carro").RoundedPrice);
    }

    [Fact]
    public void Future_version_throws_but_older_schema_still_loads()
    {
        var current = $"\"version\": {MarketStateSerializer.CurrentVersion}";

        var future = MarketStateSerializer.ToJson(new CurrencyMarket())
            .Replace(current, "\"version\": 99");
        Assert.Throws<InvalidDataException>(() => MarketStateSerializer.FromJson(future));

        // Schema antigo (v1, sem eventos) ainda carrega — agenda fica vazia.
        var legacy = MarketStateSerializer.ToJson(new CurrencyMarket())
            .Replace(current, "\"version\": 1");
        var restored = MarketStateSerializer.FromJson(legacy);
        Assert.Empty(restored.Events.All);
    }

    [Fact]
    public void Round_trip_preserves_volatility_and_floating_rate_index()
    {
        var market = new CurrencyMarket(exchangeRates: new ExchangeRateEngine(seed: 11));
        market.Currencies.Add(new Currency
        {
            Id = "Dolar", Name = "$Dolar", Symbol = "$D",
            UnitsPerMoney = 5.2m, ExchangeRateVolatilityPercent = 12m,
        });
        for (int i = 0; i < 40; i++) market.AdvanceDay();

        var restored = MarketStateSerializer.FromJson(MarketStateSerializer.ToJson(market));

        var dolar = restored.Currencies.Get("Dolar");
        Assert.Equal(12m, dolar.ExchangeRateVolatilityPercent);
        Assert.Equal(
            market.ExchangeRates.RateIndex("Dolar"),
            restored.ExchangeRates.RateIndex("Dolar"));
        Assert.Equal(market.EffectiveRate("Dolar"), restored.EffectiveRate("Dolar"));
    }

    [Fact]
    public void Round_trip_preserves_scheduled_events()
    {
        var market = new CurrencyMarket();
        market.Events.Schedule(EconomicEventLibrary.Boom(startDay: 30));
        market.Events.Schedule(EconomicEventLibrary.Crisis(startDay: 200));

        var restored = MarketStateSerializer.FromJson(MarketStateSerializer.ToJson(market));

        Assert.Equal(2, restored.Events.All.Count);
        var boom = restored.Events.All.Single(e => e.Name == "Boom");
        Assert.Equal(30, boom.StartDay);
        Assert.Equal(EconomicEventLibrary.Boom(30).GlobalInflationDelta, boom.GlobalInflationDelta);
        Assert.Equal(EconomicEventLibrary.Boom(30).IncomeMultiplier, boom.IncomeMultiplier);
        Assert.Equal(EconomicEventLibrary.Boom(30).DurationDays, boom.DurationDays);
    }

    [Fact]
    public void Blank_or_null_payload_throws()
    {
        Assert.Throws<ArgumentException>(() => MarketStateSerializer.FromJson("  "));
        Assert.Throws<InvalidDataException>(() => MarketStateSerializer.FromJson("null"));
    }
}
