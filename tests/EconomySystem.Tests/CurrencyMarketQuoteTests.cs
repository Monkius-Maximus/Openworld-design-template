using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class CurrencyMarketQuoteTests
{
    private static CurrencyMarket MarketWithDolar(decimal unitsPerMoney = 5.2m)
    {
        var market = new CurrencyMarket();
        market.Currencies.Add(new Currency
        {
            Id = "Dolar",
            Name = "$Dolar",
            Symbol = "$D",
            UnitsPerMoney = unitsPerMoney,
        });
        return market;
    }

    [Fact]
    public void Car_example_converts_at_plain_exchange_rate()
    {
        var market = MarketWithDolar(5.2m);

        var quote = market.Quote(MarketRules.SamplePreviewPriceMoney, "Dolar");

        Assert.Equal(10_000, quote.BasePriceInMoney);
        Assert.Equal(10_000m, quote.EffectiveMoneyPrice);
        Assert.Equal(52_000, quote.RoundedPrice);
        Assert.Equal("$D", quote.CurrencySymbol);
    }

    [Fact]
    public void Quote_in_base_currency_is_identity_without_inflation()
    {
        var market = MarketWithDolar();
        var quote = market.Quote(10_000, MarketRules.BaseCurrencyId);
        Assert.Equal(10_000, quote.RoundedPrice);
        Assert.Equal(MarketRules.BaseCurrencySymbol, quote.CurrencySymbol);
    }

    [Fact]
    public void Global_inflation_raises_quotes_in_every_currency()
    {
        var market = MarketWithDolar(5.2m);
        market.Inflation.GlobalAnnualPercent = 10m;
        for (int i = 0; i < MarketRules.DaysPerYear; i++)
            market.AdvanceDay();

        var emMoney = market.Quote(10_000, MarketRules.BaseCurrencyId);
        var emDolar = market.Quote(10_000, "Dolar");

        // ≈ ×1.10 nas duas moedas (tolerância pelo fator diário em double).
        Assert.InRange(emMoney.RoundedPrice, 10_990, 11_010);
        Assert.InRange(emDolar.RoundedPrice, 57_150, 57_250);
    }

    [Fact]
    public void Product_inflation_raises_only_that_product()
    {
        var market = MarketWithDolar(5.2m);
        market.Inflation.SetProductInflation("carro", 10m);
        for (int i = 0; i < MarketRules.DaysPerYear; i++)
            market.AdvanceDay();

        var carro = market.Quote(10_000, MarketRules.BaseCurrencyId, productId: "carro");
        var sofa = market.Quote(10_000, MarketRules.BaseCurrencyId, productId: "sofa");

        Assert.InRange(carro.RoundedPrice, 10_990, 11_010);
        Assert.Equal(10_000, sofa.RoundedPrice);
    }

    [Fact]
    public void Rounding_is_away_from_zero_at_final_step_only()
    {
        var market = new CurrencyMarket();
        market.Currencies.Add(new Currency
        {
            Id = "Meia",
            Name = "$Meia",
            Symbol = "$½",
            UnitsPerMoney = 0.0005m,
        });

        // 1000 × 0.0005 = 0.5 → AwayFromZero arredonda para 1.
        var quote = market.Quote(1_000, "Meia");
        Assert.Equal(0.5m, quote.ConvertedPrice);
        Assert.Equal(1, quote.RoundedPrice);
    }

    [Fact]
    public void Quote_for_unknown_currency_throws()
    {
        var market = new CurrencyMarket();
        Assert.Throws<KeyNotFoundException>(() => market.Quote(100, "Euro"));
    }

    [Fact]
    public void Negative_base_price_throws()
    {
        var market = new CurrencyMarket();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            market.Quote(-1, MarketRules.BaseCurrencyId));
    }
}
