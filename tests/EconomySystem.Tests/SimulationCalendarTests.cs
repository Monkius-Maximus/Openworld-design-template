using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class SimulationCalendarTests
{
    [Fact]
    public void Starts_at_day_zero_year_zero_monday()
    {
        var cal = new SimulationCalendar();
        Assert.Equal(0, cal.CurrentDay);
        Assert.Equal(0, cal.CurrentYear);
        Assert.Equal(DayOfWeek.Monday, cal.DayOfWeek);
    }

    [Fact]
    public void AdvanceDay_increments_day_and_weekday_cycles()
    {
        var cal = new SimulationCalendar();
        cal.AdvanceDay();
        Assert.Equal(1, cal.CurrentDay);
        Assert.Equal(DayOfWeek.Tuesday, cal.DayOfWeek);

        for (int i = 0; i < 6; i++) cal.AdvanceDay();
        Assert.Equal(7, cal.CurrentDay);
        Assert.Equal(DayOfWeek.Monday, cal.DayOfWeek);
    }

    [Fact]
    public void Year_turns_exactly_at_DaysPerYear()
    {
        var cal = new SimulationCalendar(MarketRules.DaysPerYear - 1);
        Assert.Equal(0, cal.CurrentYear);

        cal.AdvanceDay();
        Assert.Equal(MarketRules.DaysPerYear, cal.CurrentDay);
        Assert.Equal(1, cal.CurrentYear);
    }

    [Fact]
    public void Year_length_is_whole_weeks_so_weekday_repeats_every_year()
    {
        var day0 = new SimulationCalendar(0);
        var day364 = new SimulationCalendar(MarketRules.DaysPerYear);
        Assert.Equal(day0.DayOfWeek, day364.DayOfWeek);
    }

    [Fact]
    public void Negative_start_day_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationCalendar(-1));
    }
}
