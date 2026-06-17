using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class RandomEventGeneratorTests
{
    private static List<string> GenerateOverDays(int seed, double chance, int days)
    {
        var market = new CurrencyMarket();
        var generator = new RandomEventGenerator(seed, chance);
        var fired = new List<string>();
        for (int i = 0; i < days; i++)
        {
            market.AdvanceDay();
            var ev = generator.MaybeGenerate(market);
            if (ev is not null)
                fired.Add($"{ev.Name}@{ev.StartDay}");
        }
        return fired;
    }

    [Fact]
    public void Same_seed_reproduces_the_same_sequence()
    {
        var a = GenerateOverDays(seed: 7, chance: 1.0, days: 400);
        var b = GenerateOverDays(seed: 7, chance: 1.0, days: 400);

        Assert.Equal(a, b);
        Assert.True(a.Count >= 2, "400 dias > duração de um evento: deveria gerar ≥ 2");
    }

    [Fact]
    public void Zero_chance_never_generates()
    {
        Assert.Empty(GenerateOverDays(seed: 1, chance: 0.0, days: 500));
    }

    [Fact]
    public void Does_not_stack_while_an_event_is_active()
    {
        var market = new CurrencyMarket();
        var generator = new RandomEventGenerator(seed: 3, dailyChance: 1.0);

        market.AdvanceDay();
        var first = generator.MaybeGenerate(market); // dispara no dia 1
        Assert.NotNull(first);

        // Enquanto o primeiro evento está ativo, nada novo é programado.
        market.AdvanceDay();
        Assert.Null(generator.MaybeGenerate(market));
        Assert.Single(market.Events.All);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Out_of_range_chance_throws(double chance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RandomEventGenerator(0, chance));
    }
}
