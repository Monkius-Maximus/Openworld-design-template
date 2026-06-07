using EconomySystem.Core;
using EconomySystem.Core.Careers;
using Xunit;

namespace EconomySystem.Tests;

public class ChanceCardTests
{
    private static CareerState BusinessRookie() =>
        new() { CharacterId = "alice", Career = CareerLibrary.Business };

    [Fact]
    public void Option_a_applies_funds_and_promotion()
    {
        var resolver = new CareerResolver();
        var funds = new HouseholdFunds(0);
        var state = BusinessRookie();

        resolver.ResolveChanceCard("casa1", funds, state, ChanceCardLibrary.BusinessDeal, chooseA: true);

        Assert.Equal(1_200, funds.Balance);   // OptionA FundsDelta
        Assert.Equal(2, state.CurrentLevel);   // OptionA promove
    }

    [Fact]
    public void Negative_outcome_withdraws_and_demotes()
    {
        var resolver = new CareerResolver();
        var funds = new HouseholdFunds(1_000);
        var state = BusinessRookie();
        // Sobe para o nível 2 primeiro, para poder rebaixar.
        resolver.ResolveChanceCard("casa1", funds, state, ChanceCardLibrary.BusinessDeal, chooseA: true);

        var penalty = new ChanceCard
        {
            Id = "penalty",
            CareerId = "Business",
            Prompt = "Deu errado.",
            OptionA = new ChanceCardOutcome("ok", 0, CareerEffect.None),
            OptionB = new ChanceCardOutcome("ruim", -200, CareerEffect.Demote),
        };
        resolver.ResolveChanceCard("casa1", funds, state, penalty, chooseA: false);

        Assert.Equal(1_200 + 1_000 - 200, funds.Balance);
        Assert.Equal(1, state.CurrentLevel); // rebaixado de 2 para 1
    }

    [Fact]
    public void Resolution_raises_event_with_chosen_outcome()
    {
        var resolver = new CareerResolver();
        var funds = new HouseholdFunds(0);
        ChanceCardOutcome? captured = null;
        resolver.ChanceCardResolved += (_, outcome) => captured = outcome;

        resolver.ResolveChanceCard("casa1", funds, BusinessRookie(), ChanceCardLibrary.BusinessDeal, chooseA: false);

        Assert.NotNull(captured);
        Assert.Equal(ChanceCardLibrary.BusinessDeal.OptionB, captured);
    }

    [Fact]
    public void Demote_never_drops_below_level_one()
    {
        var resolver = new CareerResolver();
        var funds = new HouseholdFunds(0);
        var state = BusinessRookie(); // nível 1

        var penalty = new ChanceCard
        {
            Id = "p",
            CareerId = "Business",
            Prompt = "x",
            OptionA = new ChanceCardOutcome("a", 0, CareerEffect.Demote),
            OptionB = new ChanceCardOutcome("b", 0, CareerEffect.None),
        };
        resolver.ResolveChanceCard("casa1", funds, state, penalty, chooseA: true);

        Assert.Equal(1, state.CurrentLevel);
    }

    [Fact]
    public void Library_is_indexed_case_insensitively()
    {
        Assert.True(ChanceCardLibrary.All.ContainsKey("businessdeal"));
        Assert.Same(ChanceCardLibrary.FoodCritic, ChanceCardLibrary.All["FoodCritic"]);
    }
}
