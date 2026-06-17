using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class MarketHistoryTests
{
    [Fact]
    public void Records_in_chronological_order_and_tracks_latest()
    {
        var history = new MarketHistory();
        history.Record(1, 1.01m, 1.0m);
        history.Record(2, 1.02m, 1.0m);

        Assert.Equal(2, history.Samples.Count);
        Assert.Equal(1, history.Samples[0].Day);
        Assert.Equal(2, history.Latest!.Value.Day);
        Assert.Equal(1.02m, history.Latest.Value.GlobalIndex);
    }

    [Fact]
    public void Ring_buffer_drops_oldest_past_capacity()
    {
        var history = new MarketHistory(capacity: 2);
        history.Record(1, 1.0m, 1m);
        history.Record(2, 1.1m, 1m);
        history.Record(3, 1.2m, 1m);

        Assert.Equal(2, history.Samples.Count);
        Assert.Equal(2, history.Samples[0].Day); // dia 1 descartado
        Assert.Equal(3, history.Samples[1].Day);
    }

    [Fact]
    public void Capacity_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarketHistory(capacity: 0));
    }

    [Fact]
    public void Empty_history_has_no_latest()
    {
        Assert.Null(new MarketHistory().Latest);
    }

    [Fact]
    public void Market_records_one_sample_per_advanced_day_when_attached()
    {
        var history = new MarketHistory();
        var market = new CurrencyMarket(history: history);
        market.Inflation.GlobalAnnualPercent = 10m;

        for (int i = 0; i < 5; i++)
            market.AdvanceDay();

        Assert.Equal(5, history.Samples.Count);
        Assert.Equal(market.Calendar.CurrentDay, history.Latest!.Value.Day);
        Assert.Equal(market.Inflation.GlobalIndex, history.Latest.Value.GlobalIndex);
        // Índice global crescente com inflação positiva.
        Assert.True(history.Samples[^1].GlobalIndex > history.Samples[0].GlobalIndex);
    }

    [Fact]
    public void Market_without_history_advances_without_recording()
    {
        var market = new CurrencyMarket(); // History == null
        market.AdvanceDay();
        Assert.Null(market.History);
    }
}
