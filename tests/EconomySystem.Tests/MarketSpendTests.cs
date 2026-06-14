using EconomySystem.Core;
using EconomySystem.Core.Integration;
using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class MarketSpendTests
{
    private static CurrencyMarket MarketWithDolar(decimal inflation = 0m)
    {
        var market = new CurrencyMarket();
        market.Currencies.Add(new Currency
        {
            Id = "Dolar",
            Name = "$Dolar",
            Symbol = "$D",
            UnitsPerMoney = 5.2m,
            ProjectedAnnualInflationPercent = inflation,
            CreatedOnDay = 0,
        });
        return market;
    }

    [Fact]
    public void Cost_in_base_currency_equals_effective_money_price()
    {
        var market = MarketWithDolar();
        Assert.Equal(10_000, market.CostInMoney(10_000, MarketRules.BaseCurrencyId));
    }

    [Fact]
    public void Cost_without_currency_inflation_matches_base()
    {
        // Sem inflação própria ativa, comprar em $Dolar custa o mesmo $Money.
        var market = MarketWithDolar(inflation: 0m);
        Assert.Equal(10_000, market.CostInMoney(10_000, "Dolar"));
    }

    [Fact]
    public void Currency_inflation_adds_a_real_money_premium()
    {
        var market = MarketWithDolar(inflation: 10m);
        // Atravessa a ativação (1 ano) e acumula um ano de inflação do $Dolar.
        for (int i = 0; i < MarketRules.DaysPerYear * 2; i++)
            market.AdvanceDay();

        int baseCost = market.CostInMoney(10_000, MarketRules.BaseCurrencyId);
        int dolarCost = market.CostInMoney(10_000, "Dolar");

        Assert.Equal(10_000, baseCost); // inflação global é 0
        Assert.True(dolarCost > baseCost, $"esperava prêmio de inflação, base={baseCost} dolar={dolarCost}");
        Assert.InRange(dolarCost, 10_950, 11_050); // ≈ +10%
    }

    [Fact]
    public void TryBuy_debits_money_equivalent_and_emits_event()
    {
        var market = MarketWithDolar();
        var resolver = new MarketPurchaseResolver(market);
        var funds = new HouseholdFunds(20_000);

        MoneyTransaction? captured = null;
        resolver.Purchased += (_, tx) => captured = tx;

        bool ok = resolver.TryBuy("lar", funds, 10_000, "Dolar", productId: "carro");

        Assert.True(ok);
        Assert.Equal(10_000, funds.Balance); // 20000 − 10000
        Assert.NotNull(captured);
        Assert.Equal(-10_000, captured!.Amount);
        Assert.Equal("Dolar", captured.CurrencyId);
        Assert.Equal(TransactionKind.Purchase, captured.Kind);
    }

    [Fact]
    public void TryBuy_fails_without_funds_and_leaves_balance_untouched()
    {
        var market = MarketWithDolar();
        var resolver = new MarketPurchaseResolver(market);
        var funds = new HouseholdFunds(5_000);

        bool ok = resolver.TryBuy("lar", funds, 10_000, "Dolar");

        Assert.False(ok);
        Assert.Equal(5_000, funds.Balance);
    }

    [Fact]
    public void TryBuy_free_item_succeeds_without_touching_balance()
    {
        var market = MarketWithDolar();
        var resolver = new MarketPurchaseResolver(market);
        var funds = new HouseholdFunds(100);

        Assert.True(resolver.TryBuy("lar", funds, 0, "Dolar"));
        Assert.Equal(100, funds.Balance);
    }
}
