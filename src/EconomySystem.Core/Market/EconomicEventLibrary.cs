namespace EconomySystem.Core.Market;

/// <summary>
/// Catálogo de eventos econômicos pré-ajustados (v5). Espelha o estilo das
/// outras *Library do núcleo (CareerLibrary, ChanceCardLibrary): presets
/// tunáveis que a UI e a demo podem agendar com um clique. As magnitudes vivem
/// aqui como constantes legíveis; as durações derivam de
/// <see cref="MarketRules.DaysPerYear"/>.
/// </summary>
public static class EconomicEventLibrary
{
    private const decimal RecessionInflationDelta = -3m;
    private const decimal RecessionIncomeMultiplier = 0.85m;

    private const decimal BoomInflationDelta = 4m;
    private const decimal BoomIncomeMultiplier = 1.2m;

    private const decimal CrisisInflationDelta = 50m;
    private const decimal CrisisIncomeMultiplier = 0.7m;

    /// <summary>Recessão: meio ano de pressão deflacionária e renda mais magra.</summary>
    public static EconomicEvent Recession(int startDay) => new()
    {
        Id = $"recessao-{startDay}",
        Name = "Recessão",
        StartDay = startDay,
        DurationDays = MarketRules.DefaultEventDurationDays,
        GlobalInflationDelta = RecessionInflationDelta,
        IncomeMultiplier = RecessionIncomeMultiplier,
    };

    /// <summary>Boom: meio ano de preços e renda em alta.</summary>
    public static EconomicEvent Boom(int startDay) => new()
    {
        Id = $"boom-{startDay}",
        Name = "Boom",
        StartDay = startDay,
        DurationDays = MarketRules.DefaultEventDurationDays,
        GlobalInflationDelta = BoomInflationDelta,
        IncomeMultiplier = BoomIncomeMultiplier,
    };

    /// <summary>Crise: um trimestre de hiperinflação corroendo a renda real.</summary>
    public static EconomicEvent Crisis(int startDay) => new()
    {
        Id = $"crise-{startDay}",
        Name = "Crise (hiperinflação)",
        StartDay = startDay,
        DurationDays = MarketRules.DefaultEventDurationDays / 2,
        GlobalInflationDelta = CrisisInflationDelta,
        IncomeMultiplier = CrisisIncomeMultiplier,
    };
}
