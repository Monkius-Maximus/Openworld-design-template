using EconomySystem.Core;
using Xunit;

namespace EconomySystem.Tests;

public class OwnedObjectDepreciationTests
{
    [Fact]
    public void Starts_at_purchase_price()
    {
        var obj = new OwnedObject { Id = "tv", PurchasePrice = 1000 };
        Assert.Equal(1000, obj.CurrentValue);
    }

    [Fact]
    public void Depreciates_toward_salvage_floor_and_never_below()
    {
        var obj = new OwnedObject { Id = "tv", PurchasePrice = 1000 };
        int floor = obj.SalvageFloor; // 40% = 400

        for (int i = 0; i < 1000; i++)
            obj.DepreciateOneDay(EconomyPhysics.DepreciationPercentPerDay);

        Assert.Equal(floor, obj.CurrentValue);
    }

    [Fact]
    public void Depreciation_reduces_value_each_day()
    {
        var obj = new OwnedObject { Id = "tv", PurchasePrice = 1000 };
        obj.DepreciateOneDay(EconomyPhysics.DepreciationPercentPerDay);
        Assert.True(obj.CurrentValue < 1000);
        Assert.True(obj.CurrentValue >= obj.SalvageFloor);
    }

    [Fact]
    public void Negative_purchase_price_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OwnedObject { Id = "x", PurchasePrice = -1 });
    }
}
