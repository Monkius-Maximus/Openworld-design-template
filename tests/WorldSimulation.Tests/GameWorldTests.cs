using EconomySystem.Core;
using EconomySystem.Core.Careers;
using RelationshipSystem.Core.Interactions;
using WorldSimulation.Core;
using Xunit;

namespace WorldSimulation.Tests;

public class GameWorldTests
{
    private const float OneDay = WorldClock.MinutesPerDay;

    [Fact]
    public void Advancing_a_day_advances_market_calendar()
    {
        var world = new GameWorld();
        Assert.Equal(0, world.Market.Calendar.CurrentDay);

        world.AdvanceMinutes(OneDay);

        Assert.Equal(1, world.Market.Calendar.CurrentDay);
    }

    [Fact]
    public void Decay_and_normalization_run_on_schedule()
    {
        // começa às 8h; num dia: normaliza às 14h, 20h e 8h do dia seguinte
        // (+3 cada) e decai o daily à meia-noite (-2).
        var world = new GameWorld();
        var rel = world.Relationships.Get("a", "b");
        rel.Value.ApplyDaily(50f);

        world.AdvanceMinutes(OneDay);

        Assert.Equal(48f, rel.Value.Daily, 3);
        Assert.Equal(9f, rel.Value.Lifetime, 3);
    }

    [Fact]
    public void Talk_applies_daily_gain()
    {
        var world = DemoWorldFactory.Create();

        var outcome = world.Perform(DemoWorldFactory.PlayerId, DemoWorldFactory.AliceId, InteractionLibrary.Talk);

        Assert.True(outcome.Performed);
        Assert.True(outcome.Accepted);
        var rel = world.Relationships.Get(DemoWorldFactory.PlayerId, DemoWorldFactory.AliceId);
        Assert.Equal(3f, rel.Value.Daily, 3);
    }

    [Fact]
    public void Unavailable_interaction_returns_outcome_instead_of_throwing()
    {
        var world = DemoWorldFactory.Create();

        // Compliment exige daily >= 20; relacionamento novo está em 0.
        var outcome = world.Perform(DemoWorldFactory.PlayerId, DemoWorldFactory.BrunoId, InteractionLibrary.Compliment);

        Assert.False(outcome.Performed);
        Assert.Contains("indisponível", outcome.Message);
    }

    [Fact]
    public void Gift_debits_funds_and_applies_modifier()
    {
        var world = DemoWorldFactory.Create();
        var home = world.HouseholdOf(DemoWorldFactory.PlayerId)!;
        int before = home.Funds.Balance;

        var outcome = world.GiveGift(DemoWorldFactory.PlayerId, DemoWorldFactory.AliceId);

        Assert.True(outcome.Performed);
        Assert.True(outcome.Accepted);
        Assert.Equal(before - SocialCosts.GiftCost, home.Funds.Balance);

        var rel = world.Relationships.Get(DemoWorldFactory.PlayerId, DemoWorldFactory.AliceId);
        Assert.Contains(rel.Modifiers, m => m.Name == "Recebeu um presente");
    }

    [Fact]
    public void Gift_without_funds_cancels_without_touching_relationship()
    {
        var world = DemoWorldFactory.Create();
        var home = world.HouseholdOf(DemoWorldFactory.PlayerId)!;
        home.Funds.ForceAdjust(-home.Funds.Balance); // zera o caixa

        var outcome = world.GiveGift(DemoWorldFactory.PlayerId, DemoWorldFactory.AliceId);

        Assert.False(outcome.Performed);
        Assert.Contains("Sem dinheiro", outcome.Message);
        var rel = world.Relationships.Get(DemoWorldFactory.PlayerId, DemoWorldFactory.AliceId);
        Assert.Equal(0f, rel.Value.Daily, 3);
    }

    [Fact]
    public void Career_wage_is_paid_on_day_tick()
    {
        var world = new GameWorld();
        var home = new Household { Id = "casa", Funds = new HouseholdFunds(500) };
        world.AddHousehold(home);
        home.Careers["w"] = new CareerState { CharacterId = "w", Career = CareerLibrary.GigCourier };

        world.AdvanceMinutes(OneDay);

        // inventário vazio → conta = 0; só o salário do nível 1 entra.
        int wage = CareerLibrary.GigCourier.LevelAt(1)!.DailyWage;
        Assert.Equal(500 + wage, home.Funds.Balance);
    }

    [Fact]
    public void Repeated_talks_form_friendship_fulfill_want_and_log_events()
    {
        var world = DemoWorldFactory.Create();
        var log = new List<string>();
        world.EventLogged += log.Add;

        for (int i = 0; i < 20; i++)
        {
            world.Perform(DemoWorldFactory.PlayerId, DemoWorldFactory.AliceId, InteractionLibrary.Talk);
            world.Perform(DemoWorldFactory.AliceId, DemoWorldFactory.PlayerId, InteractionLibrary.Talk);
        }

        Assert.True(world.Relationships.AreFriends(DemoWorldFactory.PlayerId, DemoWorldFactory.AliceId));
        Assert.True(world.WantsAndFears.MeterFor(DemoWorldFactory.PlayerId).Score > 0);
        Assert.Contains(log, m => m.Contains("amigos"));
        Assert.Contains(log, m => m.Contains("Desejo realizado"));
    }

    [Fact]
    public void Demo_world_is_fully_seeded()
    {
        var world = DemoWorldFactory.Create();

        Assert.Equal(3, world.Characters.Count);
        Assert.Equal(2, world.Households.Count);
        Assert.Equal(DemoWorldFactory.PlayerHomeId, world.HouseholdOf(DemoWorldFactory.PlayerId)!.Id);
        Assert.Equal(DemoWorldFactory.NeighborsHomeId, world.HouseholdOf(DemoWorldFactory.AliceId)!.Id);
        Assert.Equal(2, world.WantsAndFears.DesiresOf(DemoWorldFactory.PlayerId).Count);
        Assert.Equal(WorldThresholds.DemoStartingFunds,
            world.HouseholdOf(DemoWorldFactory.PlayerId)!.Funds.Balance);
    }

    [Fact]
    public void Duplicate_household_id_throws()
    {
        var world = new GameWorld();
        world.AddHousehold(new Household { Id = "casa" });
        Assert.Throws<InvalidOperationException>(() => world.AddHousehold(new Household { Id = "CASA" }));
    }

    [Fact]
    public void Character_in_unknown_household_throws()
    {
        var world = new GameWorld();
        Assert.Throws<InvalidOperationException>(() => world.AddCharacter(new RelationshipSystem.Core.CharacterTraits
        {
            Id = "x",
            Name = "X",
            Zodiac = RelationshipSystem.Core.Zodiac.Aries,
            Aspiration = RelationshipSystem.Core.Aspiration.Knowledge,
            Personality = new RelationshipSystem.Core.Personality(5, 5, 5, 5, 5),
            TurnOns = new[] { "A", "B" },
            TurnOff = "C",
            Tags = System.Array.Empty<string>(),
        }, "casa-inexistente"));
    }
}
