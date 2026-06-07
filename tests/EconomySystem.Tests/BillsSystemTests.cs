using EconomySystem.Core;
using Xunit;

namespace EconomySystem.Tests;

public class BillsSystemTests
{
    private static Household HouseWith(int objectValue, int startingFunds, int children = 0)
    {
        var h = new Household { Id = "casa1", Funds = new HouseholdFunds(startingFunds) };
        h.Inventory.Add(new OwnedObject { Id = "bens", PurchasePrice = objectValue });
        h.ChildCount = children;
        return h;
    }

    [Fact]
    public void Bill_is_object_value_times_rate()
    {
        var bills = new BillsSystem();
        var h = HouseWith(objectValue: 10_000, startingFunds: 0);
        // 10.000 × 3% = 300
        Assert.Equal(300, bills.ComputeBill(h));
    }

    [Fact]
    public void Each_child_discounts_ten_percent()
    {
        var bills = new BillsSystem();
        var h = HouseWith(objectValue: 10_000, startingFunds: 0, children: 2);
        // 300 × (100-20)% = 240
        Assert.Equal(240, bills.ComputeBill(h));
    }

    [Fact]
    public void Unpaid_bill_starts_repo_grace()
    {
        var bills = new BillsSystem();
        var h = HouseWith(objectValue: 10_000, startingFunds: 0);

        bool paid = bills.DeliverAndCharge(h);

        Assert.False(paid);
        Assert.Equal(EconomyThresholds.RepoManGraceDays, h.RepoGraceDaysRemaining);
    }

    [Fact]
    public void Grace_runs_out_and_dispatches_repo_man()
    {
        var bills = new BillsSystem();
        var h = HouseWith(objectValue: 10_000, startingFunds: 0);
        string? dispatched = null;
        bills.RepoManDispatched += id => dispatched = id;

        bills.DeliverAndCharge(h); // não paga -> tolerância = 2
        for (int i = 0; i < EconomyThresholds.RepoManGraceDays; i++)
            bills.AdvanceGrace(h);

        Assert.Equal("casa1", dispatched);
        Assert.Equal(0, h.RepoGraceDaysRemaining);
    }

    [Fact]
    public void Paid_bill_clears_grace()
    {
        var bills = new BillsSystem();
        var h = HouseWith(objectValue: 10_000, startingFunds: 1_000);

        bool paid = bills.DeliverAndCharge(h);

        Assert.True(paid);
        Assert.Equal(700, h.Funds.Balance); // 1000 - 300
        Assert.Equal(0, h.RepoGraceDaysRemaining);
    }
}
