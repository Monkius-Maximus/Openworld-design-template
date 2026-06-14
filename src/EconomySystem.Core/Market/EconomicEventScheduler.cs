namespace EconomySystem.Core.Market;

/// <summary>
/// Agenda de eventos econômicos (v5). Guarda os <see cref="EconomicEvent"/>
/// programados e agrega, para um dado dia, o efeito combinado dos que estão
/// ativos: a soma das deltas de inflação e o produto dos multiplicadores de
/// renda. É consultada pelo <see cref="CurrencyMarket"/> a cada
/// <see cref="CurrencyMarket.AdvanceDay"/>.
/// </summary>
public sealed class EconomicEventScheduler
{
    private readonly List<EconomicEvent> _events = new();

    /// <summary>Disparado sempre que um evento é agendado.</summary>
    public event Action<EconomicEvent>? Scheduled;

    /// <summary>Todos os eventos agendados (passados, ativos e futuros).</summary>
    public IReadOnlyList<EconomicEvent> All => _events;

    /// <summary>Programa um novo evento.</summary>
    public void Schedule(EconomicEvent ev)
    {
        ArgumentNullException.ThrowIfNull(ev);
        _events.Add(ev);
        Scheduled?.Invoke(ev);
    }

    /// <summary>Eventos vigentes no dia informado.</summary>
    public IEnumerable<EconomicEvent> ActiveOn(int day) =>
        _events.Where(e => e.IsActiveOn(day));

    /// <summary>Soma das deltas de inflação dos eventos ativos (0 se nenhum).</summary>
    public decimal GlobalInflationDeltaOn(int day)
    {
        decimal sum = 0m;
        foreach (var e in _events)
            if (e.IsActiveOn(day))
                sum += e.GlobalInflationDelta;
        return sum;
    }

    /// <summary>Produto dos multiplicadores de renda dos eventos ativos (1 se nenhum).</summary>
    public decimal IncomeMultiplierOn(int day)
    {
        decimal factor = 1m;
        foreach (var e in _events)
            if (e.IsActiveOn(day))
                factor *= e.IncomeMultiplier;
        return factor;
    }

    /// <summary>Remove um evento agendado pelo Id (case-insensitive).</summary>
    public bool Remove(string id) =>
        id is not null &&
        _events.RemoveAll(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;

    /// <summary>Esvazia a agenda.</summary>
    public void Clear() => _events.Clear();
}
