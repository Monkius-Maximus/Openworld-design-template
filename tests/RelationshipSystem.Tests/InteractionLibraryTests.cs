using RelationshipSystem.Core;
using RelationshipSystem.Core.Interactions;
using Xunit;

namespace RelationshipSystem.Tests;

public class InteractionLibraryTests
{
    private static Relationship Rel(float daily = 0f) =>
        BuildWith(daily);

    private static Relationship BuildWith(float daily)
    {
        var rel = new RelationshipMatrix().Get("a", "b");
        rel.Value.ApplyDaily(daily);
        return rel;
    }

    [Fact]
    public void Talk_is_available_above_enemy_threshold_and_always_accepts()
    {
        Assert.True(InteractionLibrary.Talk.Available(Rel(daily: -49f)));
        Assert.False(InteractionLibrary.Talk.Available(Rel(daily: -60f)));
        Assert.True(InteractionLibrary.Talk.Accepted(Rel()));
    }

    [Fact]
    public void Talk_deltas_match_spec()
    {
        Assert.Equal(3f, InteractionLibrary.Talk.OnAccept.DailyDelta);
        Assert.Equal(1f, InteractionLibrary.Talk.OnAccept.LifetimeDelta);
    }

    [Fact]
    public void Compliment_requires_minimum_daily_and_no_fury()
    {
        Assert.True(InteractionLibrary.Compliment.Available(Rel(daily: 20f)));
        Assert.False(InteractionLibrary.Compliment.Available(Rel(daily: 19f)));
    }

    [Fact]
    public void Compliment_is_unavailable_while_furious()
    {
        var rel = Rel(daily: 50f);
        rel.AddModifier(new RelationshipModifier
        {
            Name = Relationship.FuryModifierName,
            Value = -20f,
            RemainingHours = RelationshipPhysics.FuryDurationHours
        });

        Assert.True(rel.IsFurious);
        Assert.False(InteractionLibrary.Compliment.Available(rel));
    }

    [Fact]
    public void Flirt_is_romantic()
    {
        Assert.True(InteractionLibrary.Flirt.IsRomantic);
    }

    [Fact]
    public void Flirt_acceptance_improves_with_attraction()
    {
        // Daily fixo logo abaixo do limiar base (threshold = 30 quando attraction = 0).
        var cold = Rel(daily: 40f);   // 40/2 = 20 > 30 ? não -> rejeita
        Assert.False(InteractionLibrary.Flirt.Accepted(cold));

        var hot = Rel(daily: 40f);
        hot.AttractionScore = 100;    // empurra o score e abaixa o threshold
        Assert.True(InteractionLibrary.Flirt.Accepted(hot));
    }

    [Fact]
    public void GiveGift_carries_a_48h_modifier_and_lifetime_delta()
    {
        var effect = InteractionLibrary.GiveGift.OnAccept;

        Assert.Equal(10f, effect.DailyDelta);
        Assert.Equal(5f, effect.LifetimeDelta);
        Assert.NotNull(effect.ResultingModifier);
        Assert.Equal(48f, effect.ResultingModifier!.RemainingHours);
    }

    [Fact]
    public void Insult_produces_a_fury_modifier()
    {
        var mod = InteractionLibrary.Insult.OnAccept.ResultingModifier;

        Assert.NotNull(mod);
        Assert.Equal(Relationship.FuryModifierName, mod!.Name);
        Assert.Equal(RelationshipPhysics.FuryDurationHours, mod.RemainingHours);
        Assert.True(mod.Value < 0f);
    }

    [Fact]
    public void Insult_is_unavailable_at_total_hatred()
    {
        Assert.True(InteractionLibrary.Insult.Available(Rel(daily: -79f)));
        Assert.False(InteractionLibrary.Insult.Available(Rel(daily: -80f)));
    }

    [Theory]
    [InlineData("Talk")]
    [InlineData("Compliment")]
    [InlineData("Flirt")]
    [InlineData("GiveGift")]
    [InlineData("Insult")]
    public void All_dictionary_indexes_every_interaction_by_id(string id)
    {
        Assert.True(InteractionLibrary.All.ContainsKey(id));
        Assert.Equal(id, InteractionLibrary.All[id].Id);
    }

    [Fact]
    public void All_dictionary_lookup_is_case_insensitive()
    {
        Assert.True(InteractionLibrary.All.ContainsKey("talk"));
        Assert.Same(InteractionLibrary.Talk, InteractionLibrary.All["TALK"]);
    }
}
