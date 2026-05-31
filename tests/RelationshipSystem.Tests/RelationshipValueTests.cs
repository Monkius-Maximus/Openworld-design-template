using RelationshipSystem.Core;
using Xunit;

namespace RelationshipSystem.Tests;

public class RelationshipValueTests
{
    [Fact]
    public void Starts_at_zero()
    {
        var value = new RelationshipValue();

        Assert.Equal(0f, value.Daily);
        Assert.Equal(0f, value.Lifetime);
    }

    [Fact]
    public void ApplyDaily_accumulates()
    {
        var value = new RelationshipValue();

        value.ApplyDaily(10f);
        value.ApplyDaily(5f);

        Assert.Equal(15f, value.Daily);
    }

    [Theory]
    [InlineData(500f, 100f)]
    [InlineData(-500f, -100f)]
    public void ApplyDaily_clamps_to_range(float delta, float expected)
    {
        var value = new RelationshipValue();

        value.ApplyDaily(delta);

        Assert.Equal(expected, value.Daily);
    }

    [Theory]
    [InlineData(500f, 100f)]
    [InlineData(-500f, -100f)]
    public void ApplyLifetime_clamps_to_range(float delta, float expected)
    {
        var value = new RelationshipValue();

        value.ApplyLifetime(delta);

        Assert.Equal(expected, value.Lifetime);
    }

    [Fact]
    public void Normalize_moves_lifetime_toward_daily_by_step()
    {
        var value = new RelationshipValue();
        value.ApplyDaily(50f); // lifetime ainda 0

        value.NormalizeLifetimeTowardDaily(3f);

        Assert.Equal(3f, value.Lifetime);
    }

    [Fact]
    public void Normalize_moves_down_when_daily_is_lower()
    {
        var value = new RelationshipValue();
        value.ApplyLifetime(50f);

        value.NormalizeLifetimeTowardDaily(3f);

        Assert.Equal(47f, value.Lifetime);
    }

    [Fact]
    public void Normalize_snaps_when_diff_within_step()
    {
        var value = new RelationshipValue();
        value.ApplyDaily(2f); // diff = 2 < step 3

        value.NormalizeLifetimeTowardDaily(3f);

        Assert.Equal(2f, value.Lifetime);
        Assert.Equal(value.Daily, value.Lifetime);
    }

    [Fact]
    public void Normalize_does_not_change_daily()
    {
        var value = new RelationshipValue();
        value.ApplyDaily(40f);

        value.NormalizeLifetimeTowardDaily(3f);

        Assert.Equal(40f, value.Daily);
    }

    [Fact]
    public void Decay_moves_positive_daily_toward_zero()
    {
        var value = new RelationshipValue();
        value.ApplyDaily(10f);

        value.DecayDailyTowardZero(2f);

        Assert.Equal(8f, value.Daily);
    }

    [Fact]
    public void Decay_moves_negative_daily_toward_zero()
    {
        var value = new RelationshipValue();
        value.ApplyDaily(-10f);

        value.DecayDailyTowardZero(2f);

        Assert.Equal(-8f, value.Daily);
    }

    [Fact]
    public void Decay_snaps_to_zero_when_within_step()
    {
        var value = new RelationshipValue();
        value.ApplyDaily(1f);

        value.DecayDailyTowardZero(2f);

        Assert.Equal(0f, value.Daily);
    }

    [Fact]
    public void Decay_never_overshoots_zero()
    {
        var value = new RelationshipValue();
        value.ApplyDaily(-1f);

        value.DecayDailyTowardZero(2f);

        Assert.Equal(0f, value.Daily);
    }

    [Fact]
    public void Decay_does_not_touch_lifetime()
    {
        var value = new RelationshipValue();
        value.ApplyDaily(10f);
        value.ApplyLifetime(40f);

        value.DecayDailyTowardZero(2f);

        Assert.Equal(40f, value.Lifetime);
    }

    [Fact]
    public void Reset_clears_both_axes()
    {
        var value = new RelationshipValue();
        value.ApplyDaily(30f);
        value.ApplyLifetime(30f);

        value.Reset();

        Assert.Equal(0f, value.Daily);
        Assert.Equal(0f, value.Lifetime);
    }

    [Theory]
    [InlineData(-1f)]
    public void Negative_step_throws(float step)
    {
        var value = new RelationshipValue();

        Assert.Throws<ArgumentOutOfRangeException>(() => value.DecayDailyTowardZero(step));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.NormalizeLifetimeTowardDaily(step));
    }
}
