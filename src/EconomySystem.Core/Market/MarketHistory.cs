namespace EconomySystem.Core.Market;

/// <summary>
/// Uma amostra diária do estado do mercado (v6) para o histórico/gráfico:
/// o índice global acumulado e o fator de reajuste de renda naquele dia.
/// </summary>
public readonly record struct MarketSample(int Day, decimal GlobalIndex, decimal IncomeFactor);

/// <summary>
/// Histórico do mercado (v6): série temporal de <see cref="MarketSample"/> para
/// a UI plotar a deriva de preços/renda ao longo do tempo. Buffer circular com
/// capacidade fixa — descarta as amostras mais antigas. É observacional (não é
/// serializado): acumula durante a sessão a partir do dia atual.
/// </summary>
public sealed class MarketHistory
{
    private readonly List<MarketSample> _samples = new();
    private readonly int _capacity;

    public MarketHistory(int capacity = MarketRules.DefaultHistoryCapacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(
                nameof(capacity), capacity, "capacity must be >= 1");
        _capacity = capacity;
    }

    /// <summary>Amostras em ordem cronológica (mais antiga primeiro).</summary>
    public IReadOnlyList<MarketSample> Samples => _samples;

    /// <summary>Capacidade máxima do buffer.</summary>
    public int Capacity => _capacity;

    /// <summary>Amostra mais recente, ou null se vazio.</summary>
    public MarketSample? Latest => _samples.Count > 0 ? _samples[^1] : null;

    /// <summary>Anexa uma amostra; descarta a mais antiga se estourar a capacidade.</summary>
    public void Record(int day, decimal globalIndex, decimal incomeFactor)
    {
        _samples.Add(new MarketSample(day, globalIndex, incomeFactor));
        if (_samples.Count > _capacity)
            _samples.RemoveAt(0);
    }

    /// <summary>Esvazia o histórico.</summary>
    public void Clear() => _samples.Clear();
}
