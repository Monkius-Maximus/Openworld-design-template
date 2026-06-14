namespace EconomySystem.Core.Market;

/// <summary>
/// Um evento/choque econômico (v5): uma janela temporária que distorce o
/// mercado por <see cref="DurationDays"/> dias a partir de <see cref="StartDay"/>.
/// Enquanto ativo, soma <see cref="GlobalInflationDelta"/> pontos percentuais à
/// inflação global projetada e multiplica a renda dos domicílios por
/// <see cref="IncomeMultiplier"/> (boom &gt; 1, recessão &lt; 1). Imutável e com
/// validação fail-fast nos <c>init</c>, como <see cref="Currency"/>.
/// </summary>
public sealed class EconomicEvent
{
    private readonly string _id = string.Empty;
    private readonly string _name = string.Empty;
    private readonly int _durationDays = 1;
    private readonly decimal _incomeMultiplier = 1m;

    /// <summary>Chave única (case-insensitive). Ex.: "recessao-364".</summary>
    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Event id required", nameof(Id))
            : value;
    }

    /// <summary>Nome de exibição. Ex.: "Recessão".</summary>
    public required string Name
    {
        get => _name;
        init => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Event name required", nameof(Name))
            : value;
    }

    /// <summary>Primeiro dia de simulação em que o evento vigora (0-based).</summary>
    public int StartDay { get; init; }

    /// <summary>Duração em dias (≥ 1).</summary>
    public required int DurationDays
    {
        get => _durationDays;
        init => _durationDays = value < 1
            ? throw new ArgumentOutOfRangeException(
                nameof(DurationDays), value, "DurationDays must be >= 1")
            : value;
    }

    /// <summary>
    /// Pontos percentuais somados à inflação global anual enquanto o evento está
    /// ativo (pode ser negativo: pressão deflacionária de uma recessão).
    /// </summary>
    public decimal GlobalInflationDelta { get; init; }

    /// <summary>
    /// Fator multiplicativo sobre a renda dos domicílios enquanto ativo
    /// (&gt; 0). 1.2 = +20% de renda real (boom); 0.85 = −15% (recessão).
    /// </summary>
    public decimal IncomeMultiplier
    {
        get => _incomeMultiplier;
        init => _incomeMultiplier = value <= 0m
            ? throw new ArgumentOutOfRangeException(
                nameof(IncomeMultiplier), value, "IncomeMultiplier must be > 0")
            : value;
    }

    /// <summary>Primeiro dia em que o evento JÁ acabou (exclusivo).</summary>
    public int EndDayExclusive => StartDay + DurationDays;

    /// <summary>True se o evento vigora no dia informado.</summary>
    public bool IsActiveOn(int day) => day >= StartDay && day < EndDayExclusive;

    /// <summary>True se o evento já terminou no dia informado.</summary>
    public bool HasEndedOn(int day) => day >= EndDayExclusive;
}
