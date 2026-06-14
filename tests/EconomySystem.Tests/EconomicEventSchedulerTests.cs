using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class EconomicEventSchedulerTests
{
    private static EconomicEvent Event(string id, int start, int duration,
        decimal inflationDelta, decimal incomeMult) => new()
    {
        Id = id,
        Name = id,
        StartDay = start,
        DurationDays = duration,
        GlobalInflationDelta = inflationDelta,
        IncomeMultiplier = incomeMult,
    };

    [Fact]
    public void No_events_means_neutral_aggregates()
    {
        var scheduler = new EconomicEventScheduler();

        Assert.Empty(scheduler.ActiveOn(0));
        Assert.Equal(0m, scheduler.GlobalInflationDeltaOn(0));
        Assert.Equal(1m, scheduler.IncomeMultiplierOn(0));
    }

    [Fact]
    public void Overlapping_events_sum_inflation_and_multiply_income()
    {
        var scheduler = new EconomicEventScheduler();
        scheduler.Schedule(Event("a", start: 0, duration: 10, inflationDelta: 3m, incomeMult: 1.2m));
        scheduler.Schedule(Event("b", start: 5, duration: 10, inflationDelta: -1m, incomeMult: 0.5m));

        // Dia 7: ambos ativos → soma 3 + (−1) = 2; produto 1.2 × 0.5 = 0.6.
        Assert.Equal(2, scheduler.ActiveOn(7).Count());
        Assert.Equal(2m, scheduler.GlobalInflationDeltaOn(7));
        Assert.Equal(0.6m, scheduler.IncomeMultiplierOn(7));

        // Dia 2: só "a" ativo.
        Assert.Equal(3m, scheduler.GlobalInflationDeltaOn(2));
        Assert.Equal(1.2m, scheduler.IncomeMultiplierOn(2));

        // Dia 20: nenhum.
        Assert.Equal(0m, scheduler.GlobalInflationDeltaOn(20));
        Assert.Equal(1m, scheduler.IncomeMultiplierOn(20));
    }

    [Fact]
    public void Schedule_fires_event_and_remove_is_case_insensitive()
    {
        var scheduler = new EconomicEventScheduler();
        EconomicEvent? captured = null;
        scheduler.Scheduled += e => captured = e;

        var ev = Event("Recessao", 0, 5, -2m, 0.9m);
        scheduler.Schedule(ev);

        Assert.Same(ev, captured);
        Assert.Single(scheduler.All);

        Assert.True(scheduler.Remove("RECESSAO"));
        Assert.Empty(scheduler.All);
        Assert.False(scheduler.Remove("RECESSAO"));
    }
}
