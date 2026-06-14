namespace EconomySystem.Core.Market;

/// <summary>
/// Gerador estocástico de eventos econômicos (v6). Alimenta o
/// <see cref="EconomicEventScheduler"/> automaticamente: a cada dia, com
/// probabilidade <see cref="DailyChance"/>, agenda um preset aleatório
/// (recessão/boom/crise) começando naquele dia — desde que não haja outro
/// evento já ativo (não empilha choques). É determinístico por semente: a mesma
/// seed reproduz a mesma sequência, o que o torna testável.
/// </summary>
public sealed class RandomEventGenerator
{
    private readonly Random _rng;

    public RandomEventGenerator(int seed, double dailyChance = MarketRules.DefaultDailyEventChance)
    {
        if (dailyChance is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(
                nameof(dailyChance), dailyChance, "dailyChance must be within [0, 1]");
        _rng = new Random(seed);
        DailyChance = dailyChance;
    }

    /// <summary>Probabilidade diária (0..1) de disparar um evento.</summary>
    public double DailyChance { get; }

    /// <summary>
    /// Considera gerar um evento para o dia ATUAL do mercado. Retorna o evento
    /// agendado, ou null se nada disparou (sorteio falhou ou já há evento ativo).
    /// Chamar 1x por dia, tipicamente logo após <see cref="CurrencyMarket.AdvanceDay"/>.
    /// </summary>
    public EconomicEvent? MaybeGenerate(CurrencyMarket market)
    {
        ArgumentNullException.ThrowIfNull(market);

        int today = market.Calendar.CurrentDay;
        if (market.Events.ActiveOn(today).Any())
            return null;
        if (_rng.NextDouble() >= DailyChance)
            return null;

        var ev = PickPreset(today);
        market.Events.Schedule(ev);
        return ev;
    }

    private EconomicEvent PickPreset(int day) => _rng.Next(3) switch
    {
        0 => EconomicEventLibrary.Recession(day),
        1 => EconomicEventLibrary.Boom(day),
        _ => EconomicEventLibrary.Crisis(day),
    };
}
