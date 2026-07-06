using WorldSimulation.Core;
using Xunit;

namespace WorldSimulation.Tests;

public class WorldClockTests
{
    [Fact]
    public void Starts_at_configured_hour()
    {
        var clock = new WorldClock(startHour: 8);
        Assert.Equal(8, clock.HourOfDay);
        Assert.Equal(0, clock.MinuteOfHour);
    }

    [Fact]
    public void One_hour_fires_hour_event_once()
    {
        var clock = new WorldClock(startHour: 8);
        var hours = new List<int>();
        clock.HourElapsed += h => hours.Add(h);

        clock.Advance(60f);

        Assert.Equal(new[] { 9 }, hours);
        Assert.Equal(9, clock.HourOfDay);
    }

    [Fact]
    public void Full_day_fires_24_hours_1_day_3_normalizations()
    {
        var clock = new WorldClock(startHour: 8);
        int hours = 0, days = 0, normalizations = 0;
        clock.HourElapsed += _ => hours++;
        clock.DayElapsed += () => days++;
        clock.NormalizationDue += () => normalizations++;

        clock.Advance(WorldClock.MinutesPerDay);

        Assert.Equal(24, hours);
        Assert.Equal(1, days);
        Assert.Equal(3, normalizations);
        Assert.Equal(8, clock.HourOfDay); // deu a volta completa
    }

    [Fact]
    public void Fractional_advances_accumulate_like_one_big_advance()
    {
        var clock = new WorldClock(startHour: 8);
        int hours = 0, days = 0, normalizations = 0;
        clock.HourElapsed += _ => hours++;
        clock.DayElapsed += () => days++;
        clock.NormalizationDue += () => normalizations++;

        // um dia inteiro em frações de frame (0.5 min por chamada)
        for (int i = 0; i < 2880; i++)
            clock.Advance(0.5f);

        Assert.Equal(24, hours);
        Assert.Equal(1, days);
        Assert.Equal(3, normalizations);
    }

    [Fact]
    public void Two_days_fire_double_the_events()
    {
        var clock = new WorldClock(startHour: 8);
        int days = 0, normalizations = 0;
        clock.DayElapsed += () => days++;
        clock.NormalizationDue += () => normalizations++;

        clock.Advance(2f * WorldClock.MinutesPerDay);

        Assert.Equal(2, days);
        Assert.Equal(6, normalizations);
    }

    [Fact]
    public void Negative_minutes_throw()
    {
        var clock = new WorldClock();
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(-1f));
    }

    [Fact]
    public void Invalid_start_hour_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldClock(startHour: 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldClock(startHour: -1));
    }
}
