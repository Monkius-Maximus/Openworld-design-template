using EconomySystem.Core;
using EconomySystem.Core.Economy;
using Xunit;

namespace EconomySystem.Tests;

public class IncomeResolverTests
{
    // Atividade determinística: disponível só com habilidade >= 3; sucesso fixo.
    private static IncomeActivityDefinition AlwaysSucceeds(int reward) => new()
    {
        Id = "Test",
        DisplayName = "Test",
        Available = ctx => ctx.SkillLevel >= 3,
        Succeeds = _ => true,
        OnSuccess = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Renda de teste",
            Amount = reward,
            Kind = TransactionKind.Freelance,
        }),
        OnFailure = new IncomeOutcome(new MoneyTransaction
        {
            Reason = "Falhou",
            Amount = 1,
            Kind = TransactionKind.Freelance,
        }),
    };

    [Fact]
    public void Unavailable_activity_fails_fast()
    {
        var resolver = new IncomeResolver();
        var funds = new HouseholdFunds();

        Assert.Throws<InvalidOperationException>(() =>
            resolver.Perform("casa1", funds, AlwaysSucceeds(100), skillLevel: 1));
    }

    [Fact]
    public void Successful_activity_credits_funds_and_raises_event()
    {
        var resolver = new IncomeResolver();
        var funds = new HouseholdFunds(0);
        MoneyTransaction? earned = null;
        resolver.Earned += (_, tx) => earned = tx;

        bool ok = resolver.Perform("casa1", funds, AlwaysSucceeds(100), skillLevel: 5);

        Assert.True(ok);
        Assert.Equal(100, funds.Balance);
        Assert.NotNull(earned);
        Assert.Equal(100, earned!.Amount);
    }

    [Fact]
    public void Library_activities_are_indexed_by_id()
    {
        Assert.True(IncomeActivityLibrary.All.ContainsKey("sellpainting")); // case-insensitive
        Assert.Same(IncomeActivityLibrary.SellPainting, IncomeActivityLibrary.All["SellPainting"]);
    }

    [Fact]
    public void Null_funds_throws()
    {
        var resolver = new IncomeResolver();
        Assert.Throws<ArgumentNullException>(() =>
            resolver.Perform("casa1", null!, AlwaysSucceeds(100), skillLevel: 5));
    }
}
