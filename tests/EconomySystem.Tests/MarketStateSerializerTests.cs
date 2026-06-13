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
    public void Unknown_version_throws()
    {
        var json = MarketStateSerializer.ToJson(new CurrencyMarket())
            .Replace("\"version\": 1", "\"version\": 99");
        Assert.Throws<InvalidDataException>(() => MarketStateSerializer.FromJson(json));
    }

    [Fact]
    public void Blank_or_null_payload_throws()
    {
        Assert.Throws<ArgumentException>(() => MarketStateSerializer.FromJson("  "));
        Assert.Throws<InvalidDataException>(() => MarketStateSerializer.FromJson("null"));
    }
}
