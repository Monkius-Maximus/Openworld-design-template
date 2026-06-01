using RelationshipSystem.Core;
using Xunit;

namespace RelationshipSystem.Tests;

public class RelationshipDecaySystemTests
{
    [Fact]
    public void DailyTick_decays_daily_toward_zero_for_all()
    {
        var matrix = new RelationshipMatrix();
        var decay = new RelationshipDecaySystem();
        matrix.Get("alice", "bob").Value.ApplyDaily(10f);
        matrix.Get("bob", "alice").Value.ApplyDaily(-10f);

        decay.DailyTick(matrix);

        Assert.Equal(10f - RelationshipPhysics.DailyDecayPerDay, matrix.Get("alice", "bob").Value.Daily);
        Assert.Equal(-10f + RelationshipPhysics.DailyDecayPerDay, matrix.Get("bob", "alice").Value.Daily);
    }

    [Fact]
    public void DailyTick_does_not_touch_lifetime()
    {
        var matrix = new RelationshipMatrix();
        var decay = new RelationshipDecaySystem();
        var rel = matrix.Get("alice", "bob");
        rel.Value.ApplyDaily(10f);
        rel.Value.ApplyLifetime(40f);

        decay.DailyTick(matrix);

        Assert.Equal(40f, rel.Value.Lifetime);
    }

    [Fact]
    public void DailyTick_ages_modifiers_by_24_hours()
    {
        var matrix = new RelationshipMatrix();
        var decay = new RelationshipDecaySystem();
        var rel = matrix.Get("alice", "bob");
        rel.AddModifier(new RelationshipModifier { Name = "Gift", Value = 10f, RemainingHours = 48f });

        decay.DailyTick(matrix);

        Assert.Single(rel.Modifiers);
        Assert.Equal(24f, rel.Modifiers[0].RemainingHours);
    }

    [Fact]
    public void DailyTick_removes_modifiers_that_expire_within_a_day()
    {
        var matrix = new RelationshipMatrix();
        var decay = new RelationshipDecaySystem();
        var rel = matrix.Get("alice", "bob");
        rel.AddModifier(new RelationshipModifier { Name = "Fury", Value = -20f, RemainingHours = 12f });

        decay.DailyTick(matrix);

        Assert.Empty(rel.Modifiers);
    }

    [Fact]
    public void NormalizationTick_moves_lifetime_toward_daily()
    {
        var matrix = new RelationshipMatrix();
        var decay = new RelationshipDecaySystem();
        var rel = matrix.Get("alice", "bob");
        rel.Value.ApplyDaily(50f); // lifetime começa em 0

        decay.NormalizationTick(matrix);

        Assert.Equal(RelationshipPhysics.LifetimeNormalizationPerTick, rel.Value.Lifetime);
    }

    [Fact]
    public void NormalizationTick_does_not_change_daily()
    {
        var matrix = new RelationshipMatrix();
        var decay = new RelationshipDecaySystem();
        var rel = matrix.Get("alice", "bob");
        rel.Value.ApplyDaily(50f);

        decay.NormalizationTick(matrix);

        Assert.Equal(50f, rel.Value.Daily);
    }

    [Fact]
    public void Three_normalization_ticks_close_a_nine_point_gap()
    {
        var matrix = new RelationshipMatrix();
        var decay = new RelationshipDecaySystem();
        var rel = matrix.Get("alice", "bob");
        rel.Value.ApplyDaily(9f); // 3 ticks * 3 pontos = 9

        for (int i = 0; i < RelationshipPhysics.NormalizationTicksPerDay; i++)
            decay.NormalizationTick(matrix);

        Assert.Equal(9f, rel.Value.Lifetime);
        Assert.Equal(rel.Value.Daily, rel.Value.Lifetime);
    }

    [Fact]
    public void Null_matrix_throws_on_both_ticks()
    {
        var decay = new RelationshipDecaySystem();

        Assert.Throws<ArgumentNullException>(() => decay.DailyTick(null!));
        Assert.Throws<ArgumentNullException>(() => decay.NormalizationTick(null!));
    }
}
