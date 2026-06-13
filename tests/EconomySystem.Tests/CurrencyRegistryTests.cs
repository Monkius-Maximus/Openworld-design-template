using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class CurrencyRegistryTests
{
    private static Currency Dolar(decimal rate = 5.2m) => new()
    {
        Id = "Dolar",
        Name = "$Dolar",
        Symbol = "$D",
        UnitsPerMoney = rate,
    };

    [Fact]
    public void Registry_seeds_base_currency()
    {
        var registry = new CurrencyRegistry();
        Assert.Equal(1, registry.Count);
        Assert.True(registry.Base.IsBase);
        Assert.Equal(MarketRules.BaseCurrencyId, registry.Base.Id);
        Assert.Equal(1m, registry.Base.UnitsPerMoney);
    }

    [Fact]
    public void Add_and_get_are_case_insensitive()
    {
        var registry = new CurrencyRegistry();
        registry.Add(Dolar());

        Assert.Same(registry.Get("dolar"), registry.Get("DOLAR"));
        Assert.True(registry.TryGet("DoLaR", out _));
    }

    [Fact]
    public void Duplicate_id_throws()
    {
        var registry = new CurrencyRegistry();
        registry.Add(Dolar());
        Assert.Throws<ArgumentException>(() => registry.Add(Dolar()));
    }

    [Fact]
    public void Adding_a_second_base_throws()
    {
        var registry = new CurrencyRegistry();
        Assert.Throws<ArgumentException>(() =>
            registry.Add(Currency.CreateBase()));
    }

    [Fact]
    public void Removing_base_throws_removing_unknown_returns_false()
    {
        var registry = new CurrencyRegistry();
        Assert.Throws<InvalidOperationException>(() =>
            registry.Remove(MarketRules.BaseCurrencyId));
        Assert.False(registry.Remove("Euro"));
    }

    [Fact]
    public void Add_and_remove_fire_events()
    {
        var registry = new CurrencyRegistry();
        Currency? added = null, removed = null;
        registry.CurrencyAdded += c => added = c;
        registry.CurrencyRemoved += c => removed = c;

        registry.Add(Dolar());
        Assert.Equal("Dolar", added?.Id);

        Assert.True(registry.Remove("Dolar"));
        Assert.Equal("Dolar", removed?.Id);
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void Currency_validation_fails_fast()
    {
        Assert.Throws<ArgumentException>(() => new Currency
        { Id = " ", Name = "$X", Symbol = "X", UnitsPerMoney = 1m });
        Assert.Throws<ArgumentException>(() => new Currency
        { Id = "X", Name = "", Symbol = "X", UnitsPerMoney = 1m });
        Assert.Throws<ArgumentException>(() => new Currency
        { Id = "X", Name = "$X", Symbol = "  ", UnitsPerMoney = 1m });
        Assert.Throws<ArgumentOutOfRangeException>(() => new Currency
        { Id = "X", Name = "$X", Symbol = "X", UnitsPerMoney = 0m });
        Assert.Throws<ArgumentOutOfRangeException>(() => new Currency
        {
            Id = "X",
            Name = "$X",
            Symbol = "X",
            UnitsPerMoney = 1m,
            ProjectedAnnualInflationPercent = MarketRules.MaxAnnualInflationPercent + 1,
        });
    }

    [Fact]
    public void InflationActivationDay_is_one_year_after_creation()
    {
        var currency = Dolar();
        var fromDay30 = new Currency
        {
            Id = "Euro",
            Name = "$Euro",
            Symbol = "$E",
            UnitsPerMoney = 6m,
            CreatedOnDay = 30,
        };
        Assert.Equal(MarketRules.DaysPerYear, currency.InflationActivationDay);
        Assert.Equal(30 + MarketRules.DaysPerYear, fromDay30.InflationActivationDay);
    }
}
