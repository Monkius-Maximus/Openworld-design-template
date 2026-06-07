using EconomySystem.Core;
using Xunit;

namespace EconomySystem.Tests;

public class HouseholdFundsTests
{
    private static MoneyTransaction In(int amount) =>
        new() { Reason = "teste", Amount = amount, Kind = TransactionKind.Sale };

    private static MoneyTransaction Out(int amount) =>
        new() { Reason = "teste", Amount = amount, Kind = TransactionKind.Bill };

    [Fact]
    public void Deposit_increases_balance()
    {
        var funds = new HouseholdFunds(100);
        funds.Deposit(In(50));
        Assert.Equal(150, funds.Balance);
    }

    [Fact]
    public void Withdraw_succeeds_when_enough_balance()
    {
        var funds = new HouseholdFunds(100);
        bool ok = funds.TryWithdraw(Out(-40));
        Assert.True(ok);
        Assert.Equal(60, funds.Balance);
    }

    [Fact]
    public void Withdraw_fails_and_leaves_balance_when_insufficient()
    {
        var funds = new HouseholdFunds(30);
        bool ok = funds.TryWithdraw(Out(-40));
        Assert.False(ok);
        Assert.Equal(30, funds.Balance); // inalterado, sem dívida
    }

    [Fact]
    public void Deposit_with_non_positive_amount_throws()
    {
        var funds = new HouseholdFunds();
        Assert.Throws<ArgumentException>(() => funds.Deposit(Out(-10)));
    }

    [Fact]
    public void Transaction_without_reason_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new MoneyTransaction { Reason = "  ", Amount = 1, Kind = TransactionKind.Sale });
    }

    [Fact]
    public void History_records_applied_transactions()
    {
        var funds = new HouseholdFunds(100);
        funds.Deposit(In(20));
        funds.TryWithdraw(Out(-5));
        Assert.Equal(2, funds.History.Count);
    }
}
