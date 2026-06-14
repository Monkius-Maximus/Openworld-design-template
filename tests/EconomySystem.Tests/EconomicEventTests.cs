using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class EconomicEventTests
{
    private static EconomicEvent Sample(int start = 10, int duration = 5) => new()
    {
        Id = "evento",
        Name = "Evento",
        StartDay = start,
        DurationDays = duration,
        GlobalInflationDelta = 3m,
        IncomeMultiplier = 1.1m,
    };

    [Fact]
    public void Active_window_is_half_open_start_inclusive_end_exclusive()
    {
        var ev = Sample(start: 10, duration: 5); // ativo nos dias 10..14

        Assert.False(ev.IsActiveOn(9));
        Assert.True(ev.IsActiveOn(10));
        Assert.True(ev.IsActiveOn(14));
        Assert.False(ev.IsActiveOn(15));
        Assert.Equal(15, ev.EndDayExclusive);
        Assert.True(ev.HasEndedOn(15));
        Assert.False(ev.HasEndedOn(14));
    }

    [Theory]
    [InlineData("", "Nome")]
    [InlineData("id", "")]
    public void Blank_id_or_name_throws(string id, string name)
    {
        Assert.Throws<ArgumentException>(() => new EconomicEvent
        {
            Id = id,
            Name = name,
            DurationDays = 1,
        });
    }

    [Fact]
    public void Non_positive_duration_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EconomicEvent
        {
            Id = "e", Name = "E", DurationDays = 0,
        });
    }

    [Fact]
    public void Non_positive_income_multiplier_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EconomicEvent
        {
            Id = "e", Name = "E", DurationDays = 1, IncomeMultiplier = 0m,
        });
    }

    [Fact]
    public void Library_presets_carry_expected_sign_and_windows()
    {
        var recessao = EconomicEventLibrary.Recession(0);
        var boom = EconomicEventLibrary.Boom(0);
        var crise = EconomicEventLibrary.Crisis(0);

        Assert.True(recessao.GlobalInflationDelta < 0m);
        Assert.True(recessao.IncomeMultiplier < 1m);

        Assert.True(boom.GlobalInflationDelta > 0m);
        Assert.True(boom.IncomeMultiplier > 1m);

        // Crise é mais curta (um trimestre) e mais agressiva que recessão/boom.
        Assert.True(crise.DurationDays < recessao.DurationDays);
        Assert.True(crise.GlobalInflationDelta > boom.GlobalInflationDelta);
    }
}
