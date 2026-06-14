using EconomySystem.Core.Market;
using Xunit;

namespace EconomySystem.Tests;

public class CurrencyMarketEventTests
{
    [Fact]
    public void Event_inflation_delta_only_compounds_during_its_window()
    {
        // Dois mercados idênticos; um sofre um boom de inflação no meio.
        var baseline = new CurrencyMarket();
        var withBoom = new CurrencyMarket();
        withBoom.Events.Schedule(new EconomicEvent
        {
            Id = "boom", Name = "Boom",
            StartDay = 1, DurationDays = 30,
            GlobalInflationDelta = 12m, // só durante a janela
        });

        for (int i = 0; i < 30; i++)
        {
            baseline.AdvanceDay();
            withBoom.AdvanceDay();
        }

        // Durante a janela o índice com boom subiu acima do baseline (=1, sem inflação).
        Assert.Equal(1m, baseline.Inflation.GlobalIndex);
        Assert.True(withBoom.Inflation.GlobalIndex > 1m);

        var afterWindow = withBoom.Inflation.GlobalIndex;

        // Passada a janela, o índice congela (delta volta a 0).
        for (int i = 0; i < 60; i++)
            withBoom.AdvanceDay();

        Assert.Equal(afterWindow, withBoom.Inflation.GlobalIndex);
    }

    [Fact]
    public void Income_factor_reflects_active_event_multiplier()
    {
        var market = new CurrencyMarket();
        market.Events.Schedule(new EconomicEvent
        {
            Id = "rec", Name = "Recessão",
            StartDay = 1, DurationDays = 5,
            IncomeMultiplier = 0.8m,
        });

        market.AdvanceDay(); // dia 1: recessão ativa, sem inflação acumulada ainda
        Assert.Equal(0.8m, market.IncomeAdjustmentFactor());

        for (int i = 0; i < 10; i++)
            market.AdvanceDay(); // sai da janela

        // Sem inflação global e sem evento, o reajuste volta a ser neutro.
        Assert.Equal(1m, market.IncomeAdjustmentFactor());
    }

    [Fact]
    public void Extreme_negative_event_delta_is_clamped_and_factor_stays_finite()
    {
        // Delta absurdamente negativa não pode gerar base negativa no fator diário.
        var market = new CurrencyMarket();
        market.Events.Schedule(new EconomicEvent
        {
            Id = "colapso", Name = "Colapso",
            StartDay = 1, DurationDays = 10,
            GlobalInflationDelta = -10_000m,
        });

        for (int i = 0; i < 10; i++)
            market.AdvanceDay();

        // Índice grampeado em MinAnnualInflationPercent: cai, mas continua > 0 e finito.
        Assert.True(market.Inflation.GlobalIndex > 0m);
        Assert.True(market.Inflation.GlobalIndex < 1m);
    }
}
