using EconomySystem.Core;
using EconomySystem.Core.Businesses;
using Xunit;

namespace EconomySystem.Tests;

public class BusinessResolverTests
{
    [Fact]
    public void Restock_buys_stock_and_debits_funds()
    {
        var resolver = new BusinessResolver();
        var funds = new HouseholdFunds(1_000);
        var biz = new Business { Id = "loja", OwnerId = "owner" };

        bool ok = resolver.Restock(biz, funds, "camisa", unitCost: 50, quantity: 4);

        Assert.True(ok);
        Assert.Equal(800, funds.Balance); // 1000 - 200
        Assert.Equal(4, biz.Stock["camisa"].Quantity);
    }

    [Fact]
    public void Restock_fails_without_funds()
    {
        var resolver = new BusinessResolver();
        var funds = new HouseholdFunds(100);
        var biz = new Business { Id = "loja", OwnerId = "owner" };

        bool ok = resolver.Restock(biz, funds, "camisa", unitCost: 50, quantity: 4);

        Assert.False(ok);
        Assert.Empty(biz.Stock);
        Assert.Equal(100, funds.Balance);
    }

    [Fact]
    public void Sell_applies_markup_and_records_loyalty()
    {
        var resolver = new BusinessResolver();
        var funds = new HouseholdFunds(0);
        var biz = new Business { Id = "loja", OwnerId = "owner", MarkupPercent = 20 };
        biz.Stock["camisa"] = new StockItem { Id = "camisa", UnitCost = 50, Quantity = 1 };

        int profit = resolver.SellTo(biz, funds, "cliente", "camisa", customerBuys: true);

        // preço = 50 × 1.2 = 60; lucro = 10
        Assert.Equal(10, profit);
        Assert.Equal(60, funds.Balance);
        Assert.Equal(0, biz.Stock["camisa"].Quantity);
        Assert.Equal(60, biz.Loyalty.SpendOf("cliente"));
    }

    [Fact]
    public void Sell_without_stock_fails_fast()
    {
        var resolver = new BusinessResolver();
        var funds = new HouseholdFunds(0);
        var biz = new Business { Id = "loja", OwnerId = "owner" };

        Assert.Throws<InvalidOperationException>(() =>
            resolver.SellTo(biz, funds, "cliente", "camisa", customerBuys: true));
    }

    [Fact]
    public void Loyalty_stars_grow_with_total_spend()
    {
        Assert.Equal(0, CustomerLoyalty.StarsFor(0));
        Assert.Equal(0, CustomerLoyalty.StarsFor(100));   // abaixo do 1º limiar
        Assert.Equal(1, CustomerLoyalty.StarsFor(500));
        Assert.Equal(4, CustomerLoyalty.StarsFor(10_000));
        Assert.Equal(5, CustomerLoyalty.StarsFor(20_000));
    }
}
