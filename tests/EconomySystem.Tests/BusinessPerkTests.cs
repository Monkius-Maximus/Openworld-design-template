using EconomySystem.Core;
using EconomySystem.Core.Businesses;
using Xunit;

namespace EconomySystem.Tests;

public class BusinessPerkTests
{
    private static Business RankedBusiness(int totalSpend)
    {
        var biz = new Business { Id = "loja", OwnerId = "owner" };
        if (totalSpend > 0)
            biz.Loyalty.RecordSpend("clientela", totalSpend);
        return biz;
    }

    [Fact]
    public void Wholesale_discount_unlocks_at_rank_two()
    {
        var biz = RankedBusiness(2_000); // 2 estrelas

        Assert.Equal(2, biz.Rank);
        Assert.Contains(BusinessPerk.WholesaleDiscount, biz.UnlockedPerks);
        Assert.Equal(BusinessPerks.WholesaleDiscountPercent, biz.RestockDiscountPercent);
    }

    [Fact]
    public void No_perks_at_rank_zero()
    {
        var biz = RankedBusiness(0);

        Assert.Equal(0, biz.Rank);
        Assert.Empty(biz.UnlockedPerks);
        Assert.Equal(0, biz.RestockDiscountPercent);
    }

    [Fact]
    public void Restock_applies_wholesale_discount()
    {
        var resolver = new BusinessResolver();
        var funds = new HouseholdFunds(1_000);
        var biz = RankedBusiness(2_000); // desconto de 15%

        bool ok = resolver.Restock(biz, funds, "camisa", unitCost: 100, quantity: 4);

        Assert.True(ok);
        // bruto 400, com 15% off = 340
        Assert.Equal(660, funds.Balance);
    }

    [Fact]
    public void PayEmployees_debits_total_payroll()
    {
        var resolver = new BusinessResolver();
        var funds = new HouseholdFunds(1_000);
        var biz = new Business { Id = "loja", OwnerId = "owner" };
        biz.Employees.Add(new BusinessEmployee { CharacterId = "e1", Role = EmployeeRole.Restocker });
        biz.Employees.Add(new BusinessEmployee { CharacterId = "e2", Role = EmployeeRole.Cashier });

        int paid = resolver.PayEmployees("casa1", funds, biz);

        // 80 (restocker) + 90 (cashier) = 170
        Assert.Equal(170, paid);
        Assert.Equal(830, funds.Balance);
    }

    [Fact]
    public void Tick_pays_business_payroll()
    {
        var tick = new EconomyTickSystem(new BillsSystem());
        var h = new Household { Id = "casa1", Funds = new HouseholdFunds(1_000) };
        var biz = new Business { Id = "loja", OwnerId = "alice" };
        biz.Employees.Add(new BusinessEmployee { CharacterId = "e1", Role = EmployeeRole.Sales });
        h.Businesses.Add(biz);

        tick.DailyTick(h, DayOfWeek.Monday); // segunda: sem conta

        Assert.Equal(890, h.Funds.Balance); // 1000 - 110 (sales)
    }
}
