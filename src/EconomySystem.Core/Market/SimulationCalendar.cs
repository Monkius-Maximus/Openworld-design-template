namespace EconomySystem.Core.Market;

/// <summary>
/// Calendário da simulação: conta dias corridos e deriva ano e dia da semana.
/// Dá ao <c>gameDay</c> das transações um produtor real em vez de inteiros
/// fornecidos pelo chamador. Dia 0 = segunda-feira do ano 0.
/// </summary>
public sealed class SimulationCalendar
{
    public SimulationCalendar(int startDay = 0)
    {
        if (startDay < 0)
            throw new ArgumentOutOfRangeException(
                nameof(startDay), startDay, "startDay must be >= 0");
        CurrentDay = startDay;
    }

    /// <summary>Dia corrido da simulação (0-based).</summary>
    public int CurrentDay { get; private set; }

    /// <summary>Ano da simulação (0-based), a cada <see cref="MarketRules.DaysPerYear"/> dias.</summary>
    public int CurrentYear => CurrentDay / MarketRules.DaysPerYear;

    /// <summary>Dia da semana correspondente (dia 0 = segunda-feira).</summary>
    public DayOfWeek DayOfWeek =>
        (DayOfWeek)((CurrentDay + (int)System.DayOfWeek.Monday) % 7);

    /// <summary>Avança um dia.</summary>
    public void AdvanceDay() => CurrentDay++;
}
