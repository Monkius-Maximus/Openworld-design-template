namespace EconomySystem.Core.Market;

/// <summary>
/// Motor de inflação (v4). Mantém três famílias de índices compostos
/// diariamente por fator geométrico, de modo que a taxa anual projetada
/// renda exatamente o fator anual após <see cref="MarketRules.DaysPerYear"/>
/// ticks (deriva suave em vez de choque anual de preço):
/// <list type="bullet">
/// <item><b>Global</b> — afeta todos os produtos e todas as moedas; sempre ativo.</item>
/// <item><b>Por produto</b> — afeta UM produto; ativo imediatamente.</item>
/// <item><b>Por moeda</b> — afeta tudo que se cota NAQUELA moeda; só compõe a
/// partir de <see cref="Currency.InflationActivationDay"/> (1 ano de simulação
/// após a criação da moeda).</item>
/// </list>
/// </summary>
public sealed class InflationEngine
{
    private readonly Dictionary<string, decimal> _productAnnualPercent =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _productIndex =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _currencyIndex =
        new(StringComparer.OrdinalIgnoreCase);

    private decimal _globalAnnualPercent;

    /// <summary>Inflação global anual projetada, em % (limites de <c>MarketRules</c>).</summary>
    public decimal GlobalAnnualPercent
    {
        get => _globalAnnualPercent;
        set => _globalAnnualPercent = ValidateAnnualPercent(value, nameof(GlobalAnnualPercent));
    }

    /// <summary>Índice global acumulado (1m = sem deriva).</summary>
    public decimal GlobalIndex { get; private set; } = 1m;

    /// <summary>Taxas anuais por produto atualmente registradas.</summary>
    public IReadOnlyDictionary<string, decimal> ProductAnnualPercents => _productAnnualPercent;

    /// <summary>Define (ou substitui) a inflação local de um produto.</summary>
    public void SetProductInflation(string productId, decimal annualPercent)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product id required", nameof(productId));
        ValidateAnnualPercent(annualPercent, nameof(annualPercent));

        _productAnnualPercent[productId] = annualPercent;
        _productIndex.TryAdd(productId, 1m);
    }

    /// <summary>Remove a inflação local de um produto (o índice acumulado também some).</summary>
    public bool RemoveProductInflation(string productId)
    {
        if (productId is null)
            return false;
        _productIndex.Remove(productId);
        return _productAnnualPercent.Remove(productId);
    }

    /// <summary>Índice acumulado de um produto (1m se não registrado).</summary>
    public decimal ProductIndex(string productId) =>
        productId is not null && _productIndex.TryGetValue(productId, out var idx) ? idx : 1m;

    /// <summary>Índice acumulado de uma moeda (1m enquanto a inflação dela não ativa).</summary>
    public decimal CurrencyIndex(string currencyId) =>
        currencyId is not null && _currencyIndex.TryGetValue(currencyId, out var idx) ? idx : 1m;

    /// <summary>Descarta o índice acumulado de uma moeda removida do registro.</summary>
    public void ClearCurrencyIndex(string currencyId)
    {
        if (currencyId is not null)
            _currencyIndex.Remove(currencyId);
    }

    /// <summary>
    /// Compõe um dia de inflação. Chamar 1x por dia, depois de o calendário
    /// avançar. A taxa de cada moeda vem de
    /// <see cref="Currency.ProjectedAnnualInflationPercent"/> (fonte única) e
    /// só compõe quando <c>calendar.CurrentDay >= InflationActivationDay</c>.
    /// </summary>
    public void AdvanceDay(SimulationCalendar calendar, CurrencyRegistry currencies)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        ArgumentNullException.ThrowIfNull(currencies);

        GlobalIndex *= DailyFactor(GlobalAnnualPercent);

        foreach (var (productId, annualPercent) in _productAnnualPercent)
            _productIndex[productId] = ProductIndex(productId) * DailyFactor(annualPercent);

        foreach (var currency in currencies.All)
        {
            if (currency.ProjectedAnnualInflationPercent == 0m)
                continue;
            if (calendar.CurrentDay < currency.InflationActivationDay)
                continue;
            _currencyIndex[currency.Id] =
                CurrencyIndex(currency.Id) * DailyFactor(currency.ProjectedAnnualInflationPercent);
        }
    }

    /// <summary>Fator diário equivalente a uma taxa anual composta.</summary>
    internal static decimal DailyFactor(decimal annualPercent) =>
        annualPercent == 0m
            ? 1m
            : (decimal)Math.Pow(1.0 + (double)annualPercent / 100.0, 1.0 / MarketRules.DaysPerYear);

    /// <summary>Restauração de estado salvo — usada apenas pelo serializer.</summary>
    internal void RestoreState(
        decimal globalIndex,
        IReadOnlyDictionary<string, decimal> productAnnualPercents,
        IReadOnlyDictionary<string, decimal> productIndices,
        IReadOnlyDictionary<string, decimal> currencyIndices)
    {
        GlobalIndex = globalIndex;

        _productAnnualPercent.Clear();
        _productIndex.Clear();
        _currencyIndex.Clear();

        foreach (var (id, percent) in productAnnualPercents)
            _productAnnualPercent[id] = percent;
        foreach (var (id, index) in productIndices)
            _productIndex[id] = index;
        foreach (var (id, index) in currencyIndices)
            _currencyIndex[id] = index;
    }

    /// <summary>Snapshot interno para persistência.</summary>
    internal (IReadOnlyDictionary<string, decimal> ProductIndices,
              IReadOnlyDictionary<string, decimal> CurrencyIndices) SnapshotIndices() =>
        (_productIndex, _currencyIndex);

    private static decimal ValidateAnnualPercent(decimal value, string paramName) =>
        value < MarketRules.MinAnnualInflationPercent ||
        value > MarketRules.MaxAnnualInflationPercent
            ? throw new ArgumentOutOfRangeException(
                paramName, value,
                $"Annual inflation must be within [{MarketRules.MinAnnualInflationPercent}, {MarketRules.MaxAnnualInflationPercent}]%")
            : value;
}
