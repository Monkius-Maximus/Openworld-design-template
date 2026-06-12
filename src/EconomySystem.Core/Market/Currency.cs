namespace EconomySystem.Core.Market;

/// <summary>
/// Uma moeda do mercado (v4). A base ($Money) é a unidade canônica do motor;
/// as demais são camadas de câmbio/cotação criadas pelo jogador (ex.: $Dolar,
/// $Real, $Euro). Imutável e com validação fail-fast nos <c>init</c>, como
/// <c>MoneyTransaction</c>.
/// </summary>
public sealed class Currency
{
    private readonly string _id = string.Empty;
    private readonly string _name = string.Empty;
    private readonly string _symbol = string.Empty;
    private readonly decimal _unitsPerMoney = 1m;
    private readonly decimal _projectedAnnualInflationPercent;

    /// <summary>Chave única (case-insensitive). Ex.: "Dolar".</summary>
    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Currency id required", nameof(Id))
            : value;
    }

    /// <summary>Nome de exibição. Ex.: "$Dolar".</summary>
    public required string Name
    {
        get => _name;
        init => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Currency name required", nameof(Name))
            : value;
    }

    /// <summary>Símbolo curto usado em cotações. Ex.: "$D".</summary>
    public required string Symbol
    {
        get => _symbol;
        init => _symbol = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Currency symbol required", nameof(Symbol))
            : value;
    }

    /// <summary>Taxa de conversão: 1 $Money = N unidades desta moeda.</summary>
    public required decimal UnitsPerMoney
    {
        get => _unitsPerMoney;
        init => _unitsPerMoney = value < MarketRules.MinUnitsPerMoney
            ? throw new ArgumentOutOfRangeException(
                nameof(UnitsPerMoney), value,
                $"UnitsPerMoney must be >= {MarketRules.MinUnitsPerMoney}")
            : value;
    }

    /// <summary>
    /// Inflação anual projetada, em %. Só passa a compor depois de 1 ano de
    /// simulação a contar da criação (ver <see cref="InflationActivationDay"/>).
    /// </summary>
    public decimal ProjectedAnnualInflationPercent
    {
        get => _projectedAnnualInflationPercent;
        init => _projectedAnnualInflationPercent =
            value < MarketRules.MinAnnualInflationPercent ||
            value > MarketRules.MaxAnnualInflationPercent
                ? throw new ArgumentOutOfRangeException(
                    nameof(ProjectedAnnualInflationPercent), value,
                    $"Annual inflation must be within [{MarketRules.MinAnnualInflationPercent}, {MarketRules.MaxAnnualInflationPercent}]%")
                : value;
    }

    /// <summary>Dia de simulação em que a moeda foi criada.</summary>
    public int CreatedOnDay { get; init; }

    /// <summary>True apenas para a moeda canônica $Money.</summary>
    public bool IsBase { get; init; }

    /// <summary>Primeiro dia em que a inflação projetada passa a compor.</summary>
    public int InflationActivationDay => CreatedOnDay + MarketRules.DaysPerYear;

    /// <summary>Cria a moeda base $Money (taxa 1:1, sem inflação própria).</summary>
    public static Currency CreateBase(int createdOnDay = 0) => new()
    {
        Id = MarketRules.BaseCurrencyId,
        Name = MarketRules.BaseCurrencyName,
        Symbol = MarketRules.BaseCurrencySymbol,
        UnitsPerMoney = 1m,
        CreatedOnDay = createdOnDay,
        IsBase = true,
    };
}
