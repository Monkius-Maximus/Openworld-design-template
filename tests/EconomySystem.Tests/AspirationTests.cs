using EconomySystem.Core;
using EconomySystem.Core.Aspiration;
using Xunit;

namespace EconomySystem.Tests;

public class AspirationTests
{
    [Fact]
    public void Earn_and_spend_points()
    {
        var wallet = new AspirationWallet(1_000);
        wallet.Earn(500);
        Assert.Equal(1_500, wallet.Points);

        bool ok = wallet.TrySpend(1_200);
        Assert.True(ok);
        Assert.Equal(300, wallet.Points);
    }

    [Fact]
    public void Spending_more_than_balance_fails()
    {
        var wallet = new AspirationWallet(100);
        Assert.False(wallet.TrySpend(101));
        Assert.Equal(100, wallet.Points); // inalterado
    }

    [Fact]
    public void Redeem_reward_spends_points_and_raises_event()
    {
        var wallet = new AspirationWallet(20_000);
        var resolver = new AspirationResolver();
        AspirationReward? redeemed = null;
        resolver.Redeemed += (_, r) => redeemed = r;

        bool ok = resolver.TryRedeem("alice", wallet, AspirationRewardCatalog.MoneyTree);

        Assert.True(ok);
        Assert.Equal(20_000 - AspirationRewardCatalog.MoneyTree.PointCost, wallet.Points);
        Assert.Same(AspirationRewardCatalog.MoneyTree, redeemed);
    }

    [Fact]
    public void Redeem_fails_without_enough_points()
    {
        var wallet = new AspirationWallet(100);
        var resolver = new AspirationResolver();

        bool ok = resolver.TryRedeem("alice", wallet, AspirationRewardCatalog.MoneyTree);

        Assert.False(ok);
        Assert.Equal(100, wallet.Points);
    }

    [Fact]
    public void Catalog_is_indexed_case_insensitively()
    {
        Assert.True(AspirationRewardCatalog.All.ContainsKey("moneytree"));
        Assert.Equal(RewardBenefit.Money, AspirationRewardCatalog.MoneyTree.Benefit);
    }

    [Fact]
    public void Reward_requires_positive_point_cost()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AspirationReward
        {
            Id = "bad",
            Name = "Ruim",
            PointCost = 0,
            Benefit = RewardBenefit.Motives,
        });
    }
}
