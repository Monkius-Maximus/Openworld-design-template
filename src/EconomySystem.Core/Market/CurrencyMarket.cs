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
        EconomicEventScheduler? events = null,
        MarketHistory? history = null,
        ExchangeRateEngine? exchangeRates = null)
    {
        Calendar = calendar ?? new SimulationCalendar();
        Currencies = currencies ?? new CurrencyRegistry();
        Inflation = inflation ?? new InflationEngine();
        Events = events ?? new EconomicEventScheduler();
        History = history;
        ExchangeRates = exchangeRates ?? new ExchangeRateEngine();
    }

    public SimulationCalendar Calendar { get; }
    public CurrencyRegistry Currencies { get; }
    public InflationEngine Inflation { get; }

    /// <summary>Agenda de eventos/choques econômicos (v5).</summary>
    public EconomicEventScheduler Events { get; }

    /// <summary>Histórico opcional (v6): se presente, grava uma amostra por dia.</summary>
    public MarketHistory? History { get; }

    /// <summary>Motor de câmbio flutuante (v7): inerte se nenhuma moeda tem volatilidade.</summary>
    public ExchangeRateEngine ExchangeRates { get; }

    /// <summary>
    /// Avança um dia de simulação e compõe a inflação do dia, somando à deriva
    /// global a delta dos eventos econômicos ativos hoje; também faz o câmbio
    /// flutuar. Se há histórico anexado, registra a amostra do dia.
    /// </summary>
    public void AdvanceDay()
    {
        Calendar.AdvanceDay();
        Inflation.AdvanceDay(
            Calendar, Currencies, Events.GlobalInflationDeltaOn(Calendar.CurrentDay));
        ExchangeRates.AdvanceDay(Currencies);
        History?.Record(Calendar.CurrentDay, Inflation.GlobalIndex, IncomeAdjustmentFactor());
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
            * Inflation.CurrencyIndex(currency.Id)
            * ExchangeRates.RateIndex(currency.Id);

        return new PriceQuote(
            BasePriceInMoney: basePriceInMoney,
            EffectiveMoneyPrice: effectiveMoney,
            ConvertedPrice: converted,
            RoundedPrice: (int)Math.Round(converted, MidpointRounding.AwayFromZero),
            CurrencySymbol: currency.Symbol);
    }

    /// <summary>
    /// Taxa de câmbio EFETIVA de hoje (v7): a nominal da moeda já com o índice
    /// de inflação própria e o índice de câmbio flutuante. Para a base é 1.
    /// </summary>
    public decimal EffectiveRate(string currencyId)
    {
        var currency = Currencies.Get(currencyId);
        return currency.UnitsPerMoney
            * Inflation.CurrencyIndex(currency.Id)
            * ExchangeRates.RateIndex(currency.Id);
    }

    /// <summary>
    /// Quanto custa, em $Money inteiros, comprar um item DENOMINADO em outra
    /// moeda (v6): cota o preço naquela moeda (o inteiro que o jogador vê) e
    /// converte de volta pelo câmbio. Para a base é o próprio preço efetivo;
    /// para moedas com inflação própria ativa, embute o prêmio de inflação
    /// delas (gastar numa moeda inflacionada custa mais $Money de verdade).
    /// </summary>
    public int CostInMoney(int basePriceInMoney, string currencyId, string? productId = null)
    {
        var quote = Quote(basePriceInMoney, currencyId, productId);
        var currency = Currencies.Get(currencyId);
        return (int)Math.Round(
            quote.RoundedPrice / currency.UnitsPerMoney, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Remove uma moeda do registro E limpa os índices acumulados dela
    /// (inflação própria e câmbio flutuante); recriar a moeda recomeça do zero.
    /// </summary>
    public bool RemoveCurrency(string id)
    {
        if (!Currencies.Remove(id))
            return false;
        Inflation.ClearCurrencyIndex(id);
        ExchangeRates.ClearRateIndex(id);
        return true;
    }
}
