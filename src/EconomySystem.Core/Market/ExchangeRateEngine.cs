namespace EconomySystem.Core.Market;

/// <summary>
/// Motor de câmbio flutuante (v7). Mantém um índice multiplicativo por moeda
/// (1m = sem deriva) que caminha por um <b>random walk</b> simétrico a cada dia,
/// com amplitude dada pela <see cref="Currency.ExchangeRateVolatilityPercent"/>
/// (fonte única, como o motor de inflação lê a inflação da própria moeda). O
/// índice é grampeado à banda <c>[MinRateIndex, MaxRateIndex]</c> para o câmbio
/// não disparar. A moeda base e moedas com volatilidade 0 ficam inertes (1m),
/// então mercados existentes não mudam de comportamento. Determinístico por
/// semente — testável.
/// </summary>
public sealed class ExchangeRateEngine
{
    private readonly Dictionary<string, decimal> _rateIndex =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Random _rng;

    public ExchangeRateEngine(int seed = 0) => _rng = new Random(seed);

    /// <summary>Índice de câmbio acumulado de uma moeda (1m se sem deriva).</summary>
    public decimal RateIndex(string currencyId) =>
        currencyId is not null && _rateIndex.TryGetValue(currencyId, out var idx) ? idx : 1m;

    /// <summary>
    /// Compõe um dia de flutuação para cada moeda com volatilidade &gt; 0.
    /// Chamar 1x por dia, junto do avanço do calendário/inflação.
    /// </summary>
    public void AdvanceDay(CurrencyRegistry currencies)
    {
        ArgumentNullException.ThrowIfNull(currencies);

        foreach (var currency in currencies.All)
        {
            if (currency.IsBase || currency.ExchangeRateVolatilityPercent == 0m)
                continue;

            // Passo simétrico em [-vol, +vol]% sobre o índice atual.
            double amplitude = (double)currency.ExchangeRateVolatilityPercent / 100.0;
            double step = (_rng.NextDouble() * 2.0 - 1.0) * amplitude;
            decimal next = RateIndex(currency.Id) * (decimal)(1.0 + step);

            _rateIndex[currency.Id] = Math.Clamp(
                next, MarketRules.MinRateIndex, MarketRules.MaxRateIndex);
        }
    }

    /// <summary>Descarta o índice de câmbio de uma moeda removida do registro.</summary>
    public void ClearRateIndex(string currencyId)
    {
        if (currencyId is not null)
            _rateIndex.Remove(currencyId);
    }

    /// <summary>Restauração de estado salvo — usada apenas pelo serializer.</summary>
    internal void RestoreState(IReadOnlyDictionary<string, decimal> rateIndices)
    {
        _rateIndex.Clear();
        foreach (var (id, index) in rateIndices)
            _rateIndex[id] = index;
    }

    /// <summary>Snapshot interno para persistência.</summary>
    internal IReadOnlyDictionary<string, decimal> SnapshotIndices() => _rateIndex;
}
