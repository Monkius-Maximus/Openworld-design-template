using System.Text.Json;

namespace EconomySystem.Core.Market;

/// <summary>
/// Persistência do mercado (v4) como round-trip puro de string JSON — sem IO,
/// para ser testável e agnóstico de engine (a camada Godot é dona do arquivo).
/// Persiste os ÍNDICES acumulados, não só as taxas, para que recarregar no
/// meio de um ano não zere a deriva de inflação já composta.
/// </summary>
public static class MarketStateSerializer
{
    public const int CurrentVersion = 3;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Serializa o estado completo do mercado para JSON indentado.</summary>
    public static string ToJson(CurrencyMarket market)
    {
        ArgumentNullException.ThrowIfNull(market);

        var (productIndices, currencyIndices) = market.Inflation.SnapshotIndices();

        var data = new MarketSaveData
        {
            Version = CurrentVersion,
            CurrentDay = market.Calendar.CurrentDay,
            GlobalAnnualPercent = market.Inflation.GlobalAnnualPercent,
            GlobalIndex = market.Inflation.GlobalIndex,
            Currencies = market.Currencies.All.Select(c => new CurrencyDto
            {
                Id = c.Id,
                Name = c.Name,
                Symbol = c.Symbol,
                UnitsPerMoney = c.UnitsPerMoney,
                ProjectedAnnualInflationPercent = c.ProjectedAnnualInflationPercent,
                ExchangeRateVolatilityPercent = c.ExchangeRateVolatilityPercent,
                CreatedOnDay = c.CreatedOnDay,
                IsBase = c.IsBase,
            }).ToList(),
            Products = market.Inflation.ProductAnnualPercents.ToDictionary(
                kv => kv.Key,
                kv => new ProductInflationDto
                {
                    AnnualPercent = kv.Value,
                    Index = market.Inflation.ProductIndex(kv.Key),
                }),
            CurrencyIndices = new Dictionary<string, decimal>(currencyIndices),
            Events = market.Events.All.Select(e => new EconomicEventDto
            {
                Id = e.Id,
                Name = e.Name,
                StartDay = e.StartDay,
                DurationDays = e.DurationDays,
                GlobalInflationDelta = e.GlobalInflationDelta,
                IncomeMultiplier = e.IncomeMultiplier,
            }).ToList(),
            RateIndices = new Dictionary<string, decimal>(market.ExchangeRates.SnapshotIndices()),
        };

        return JsonSerializer.Serialize(data, Options);
    }

    /// <summary>
    /// Reconstrói um mercado a partir do JSON salvo. Valida a versão do
    /// schema e restaura calendário, moedas, taxas e índices acumulados. O
    /// <paramref name="history"/> opcional (v6) é anexado ao mercado
    /// reconstruído — o histórico em si é observacional e não é serializado.
    /// </summary>
    public static CurrencyMarket FromJson(string json, MarketHistory? history = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Save JSON required", nameof(json));

        var data = JsonSerializer.Deserialize<MarketSaveData>(json, Options)
            ?? throw new InvalidDataException("Save JSON desserializou como nulo.");

        // Aceita schemas antigos (v1 não tinha eventos) e o atual; rejeita
        // versões futuras desconhecidas. A v1 simplesmente carrega sem eventos.
        if (data.Version < 1 || data.Version > CurrentVersion)
            throw new InvalidDataException(
                $"Versão de save desconhecida: {data.Version} (suportadas 1..{CurrentVersion}).");

        var baseDto = data.Currencies.FirstOrDefault(c => c.IsBase)
            ?? throw new InvalidDataException("Save sem moeda base.");

        var registry = new CurrencyRegistry(ToCurrency(baseDto));
        foreach (var dto in data.Currencies.Where(c => !c.IsBase))
            registry.Add(ToCurrency(dto));

        var market = new CurrencyMarket(
            new SimulationCalendar(data.CurrentDay), registry, history: history)
        {
            Inflation = { GlobalAnnualPercent = data.GlobalAnnualPercent },
        };

        market.Inflation.RestoreState(
            data.GlobalIndex,
            data.Products.ToDictionary(kv => kv.Key, kv => kv.Value.AnnualPercent),
            data.Products.ToDictionary(kv => kv.Key, kv => kv.Value.Index),
            data.CurrencyIndices);

        foreach (var dto in data.Events)
            market.Events.Schedule(new EconomicEvent
            {
                Id = dto.Id,
                Name = dto.Name,
                StartDay = dto.StartDay,
                DurationDays = dto.DurationDays,
                GlobalInflationDelta = dto.GlobalInflationDelta,
                IncomeMultiplier = dto.IncomeMultiplier,
            });

        market.ExchangeRates.RestoreState(data.RateIndices);

        return market;
    }

    private static Currency ToCurrency(CurrencyDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Symbol = dto.Symbol,
        UnitsPerMoney = dto.UnitsPerMoney,
        ProjectedAnnualInflationPercent = dto.ProjectedAnnualInflationPercent,
        ExchangeRateVolatilityPercent = dto.ExchangeRateVolatilityPercent,
        CreatedOnDay = dto.CreatedOnDay,
        IsBase = dto.IsBase,
    };

    private sealed class MarketSaveData
    {
        public int Version { get; set; }
        public int CurrentDay { get; set; }
        public decimal GlobalAnnualPercent { get; set; }
        public decimal GlobalIndex { get; set; } = 1m;
        public List<CurrencyDto> Currencies { get; set; } = new();
        public Dictionary<string, ProductInflationDto> Products { get; set; } = new();
        public Dictionary<string, decimal> CurrencyIndices { get; set; } = new();
        public List<EconomicEventDto> Events { get; set; } = new();
        public Dictionary<string, decimal> RateIndices { get; set; } = new();
    }

    private sealed class EconomicEventDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StartDay { get; set; }
        public int DurationDays { get; set; } = 1;
        public decimal GlobalInflationDelta { get; set; }
        public decimal IncomeMultiplier { get; set; } = 1m;
    }

    private sealed class CurrencyDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal UnitsPerMoney { get; set; }
        public decimal ProjectedAnnualInflationPercent { get; set; }
        public decimal ExchangeRateVolatilityPercent { get; set; }
        public int CreatedOnDay { get; set; }
        public bool IsBase { get; set; }
    }

    private sealed class ProductInflationDto
    {
        public decimal AnnualPercent { get; set; }
        public decimal Index { get; set; } = 1m;
    }
}
