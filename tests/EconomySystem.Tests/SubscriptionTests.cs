using EconomySystem.Core;
using Xunit;

namespace EconomySystem.Tests;

public class SubscriptionTests
{
    [Fact]
    public void Advances_and_bills_at_end_of_cycle()
    {
        var sub = new Subscription { Name = "Streaming", Cost = 30 };

        bool due = false;
        for (int i = 0; i < EconomyPhysics.SubscriptionBillingDays; i++)
            due = sub.AdvanceDay();

        Assert.True(due); // vence exatamente ao fim do ciclo
    }

    [Fact]
    public void Re_arms_after_billing()
    {
        var sub = new Subscription { Name = "Celular", Cost = 50 };
        for (int i = 0; i < EconomyPhysics.SubscriptionBillingDays; i++)
            sub.AdvanceDay();

        // No dia seguinte ao débito, não vence de novo.
        Assert.False(sub.AdvanceDay());
    }

    [Fact]
    public void Tick_debits_due_subscription()
    {
        var tick = new EconomyTickSystem(new BillsSystem());
        var h = new Household { Id = "casa1", Funds = new HouseholdFunds(1_000) };
        h.Subscriptions.Add(new Subscription { Name = "Streaming", Cost = 30 });

        // Avança até o dia do débito (em dia sem conta, quarta).
        for (int i = 0; i < EconomyPhysics.SubscriptionBillingDays; i++)
            tick.DailyTick(h, DayOfWeek.Wednesday);

        Assert.Equal(970, h.Funds.Balance); // 1000 - 30
    }

    [Fact]
    public void Debit_transaction_is_negative_and_tagged()
    {
        var sub = new Subscription { Name = "Streaming", Cost = 30 };
        var tx = sub.ToDebit();
        Assert.Equal(-30, tx.Amount);
        Assert.Equal(TransactionKind.Subscription, tx.Kind);
    }
}
