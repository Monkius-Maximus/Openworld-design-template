using EconomySystem.Core;
using EconomySystem.Core.Integration;
using RelationshipSystem.Core;
using Xunit;

namespace EconomySystem.Tests;

public class PaidInteractionResolverTests
{
    [Fact]
    public void Paid_gift_withdraws_cost_and_applies_social_effect()
    {
        var matrix = new RelationshipMatrix();
        var paid = new PaidInteractionResolver(new InteractionResolver(matrix));
        var funds = new HouseholdFunds(100);

        bool accepted = paid.GiveGift("casa1", funds, "alice", "bob");

        Assert.True(accepted);
        Assert.Equal(100 - SocialCosts.GiftCost, funds.Balance);
        Assert.True(matrix.Get("alice", "bob").Value.Daily > 0); // presente surtiu efeito
    }

    [Fact]
    public void Insufficient_funds_cancels_gift_without_social_effect()
    {
        var matrix = new RelationshipMatrix();
        var paid = new PaidInteractionResolver(new InteractionResolver(matrix));
        var funds = new HouseholdFunds(50); // < GiftCost (75)
        bool cancelledFired = false;
        paid.Cancelled += (_, _) => cancelledFired = true;

        bool accepted = paid.GiveGift("casa1", funds, "alice", "bob");

        Assert.False(accepted);
        Assert.True(cancelledFired);
        Assert.Equal(50, funds.Balance);                       // nada debitado
        Assert.Equal(0f, matrix.Get("alice", "bob").Value.Daily); // relacionamento intocado
    }

    [Fact]
    public void Spent_event_reports_cost()
    {
        var matrix = new RelationshipMatrix();
        var paid = new PaidInteractionResolver(new InteractionResolver(matrix));
        var funds = new HouseholdFunds(100);
        MoneyTransaction? spent = null;
        paid.Spent += (_, tx) => spent = tx;

        paid.GiveGift("casa1", funds, "alice", "bob");

        Assert.NotNull(spent);
        Assert.Equal(-SocialCosts.GiftCost, spent!.Amount);
        Assert.Equal(TransactionKind.Gift, spent.Kind);
    }

    [Fact]
    public void Null_social_resolver_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PaidInteractionResolver(null!));
    }
}
