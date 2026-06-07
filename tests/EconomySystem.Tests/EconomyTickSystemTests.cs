using EconomySystem.Core;
using Xunit;

namespace EconomySystem.Tests;

public class EconomyTickSystemTests
{
    private static Household House()
    {
        var h = new Household { Id = "casa1", Funds = new HouseholdFunds(5_000) };
        h.Inventory.Add(new OwnedObject { Id = "tv", PurchasePrice = 1_000 });
        return h;
    }

    [Fact]
    public void DailyTick_depreciates_objects()
    {
        var tick = new EconomyTickSystem(new BillsSystem());
        var h = House();

        tick.DailyTick(h, DayOfWeek.Monday); // segunda: sem conta

        Assert.True(h.Inventory.TotalObjectValue < 1_000);
    }

    [Fact]
    public void DailyTick_charges_bill_on_delivery_day()
    {
        var tick = new EconomyTickSystem(new BillsSystem());
        var h = House();

        tick.DailyTick(h, DayOfWeek.Tuesday); // terça: conta chega

        Assert.True(h.Funds.Balance < 5_000);
    }

    [Fact]
    public void DailyTick_skips_bill_on_non_delivery_day()
    {
        var tick = new EconomyTickSystem(new BillsSystem());
        var h = House();

        tick.DailyTick(h, DayOfWeek.Wednesday); // quarta: sem conta

        Assert.Equal(5_000, h.Funds.Balance);
    }

    [Fact]
    public void Null_bills_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EconomyTickSystem(null!));
    }
}
