namespace EconomySystem.Core.Market;

/// <summary>
/// Fachada do mercado financeiro (v4): agrega calendário, registro de moedas
/// e motor de inflação. É com ela que UI, demo e persistência conversam. O
/// mercado é opt-in — nada no tick clássico
/// (<c>DailyTick(Household, DayOfWeek)</c>) muda sem ele.
/// </summary>
public sealed class CurrencyMarket
{
    public CurrencyMarket(
        SimulationCalendar? calendar = null,
        CurrencyRegistry? currencies = null,
        InflationEngine? inflation = null,
        EconomicEventScheduler? events = null)
    {
        Calendar = calendar ?? new SimulationCalendar();
        Currencies = currencies ?? new CurrencyRegistry();
        Inflation = inflation ?? new InflationEngine();
        Events = events ?? new EconomicEventScheduler();
    }

    public SimulationCalendar Calendar { get; }
    public CurrencyRegistry Currencies { get; }
    public InflationEngine Inflation { get; }

    /// <summary>Agenda de eventos/choques econômicos (v5).</summary>
    public EconomicEventScheduler Events { get; }

    /// <summary>
    /// Avança um dia de simulação e compõe a inflação do dia, somando à deriva
    /// global a delta dos eventos econômicos ativos hoje.
    /// </summary>
    public void AdvanceDay()
    {
        Calendar.AdvanceDay();
        Inflation.AdvanceDay(
            Calendar, Currencies, Events.GlobalInflationDeltaOn(Calendar.CurrentDay));
    }

    /// <summary>
    /// Fator de reajuste da renda hoje (v5): o índice global acumulado
    /// (custo de vida) vezes o multiplicador de renda dos eventos ativos. O
    /// tick clássico usa isto para indexar salários à inflação — renda real
    /// constante em tempos normais, e oscilando em boom/recessão.
    /// </summary>
    public decimal IncomeAdjustmentFactor() =>
        Inflation.GlobalIndex * Events.IncomeMultiplierOn(Calendar.CurrentDay);

    /// <summary>
    /// Cota um preço de catálogo (em $Money) numa moeda, aplicando o pipeline:
    /// inflação global → inflação local do produto (se informado) → câmbio →
    /// inflação local da moeda (ativa só após 1 ano de simulação).
    /// </summary>
    public PriceQuote Quote(int basePriceInMoney, string currencyId, string? productId = null)
    {
        if (basePriceInMoney < 0)
            throw new ArgumentOutOfRangeException(
                nameof(basePriceInMoney), basePriceInMoney, "Base price must be >= 0");

        var currency = Currencies.Get(currencyId);

        var effectiveMoney = basePriceInMoney
            * Inflation.GlobalIndex
            * (productId is null ? 1m : Inflation.ProductIndex(productId));

        var converted = effectiveMoney
            * currency.UnitsPerMoney
            * Inflation.CurrencyIndex(currency.Id);

        return new PriceQuote(
            BasePriceInMoney: basePriceInMoney,
            EffectiveMoneyPrice: effectiveMoney,
            ConvertedPrice: converted,
            RoundedPrice: (int)Math.Round(converted, MidpointRounding.AwayFromZero),
            CurrencySymbol: currency.Symbol);
    }

    /// <summary>
    /// Remove uma moeda do registro E limpa o índice de inflação acumulado
    /// dela no motor (recriar a moeda recomeça do zero).
    /// </summary>
    public bool RemoveCurrency(string id)
    {
        if (!Currencies.Remove(id))
            return false;
        Inflation.ClearCurrencyIndex(id);
        return true;
    }
}
