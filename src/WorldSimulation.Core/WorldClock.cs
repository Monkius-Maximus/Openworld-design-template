namespace WorldSimulation.Core;

/// <summary>
/// Relógio do mundo: acumula minutos de jogo e dispara os ganchos de tempo
/// que os núcleos esperam — hora cheia, normalização (3×/dia) e virada de dia
/// (meia-noite). Quem conta DIAS é o <c>SimulationCalendar</c> do mercado; o
/// relógio só cuida do intradia, evitando duas fontes de verdade.
/// </summary>
public sealed class WorldClock
{
    public const int HoursPerDay = 24;
    public const float MinutesPerDay = 1440f;

    private double _totalMinutes;
    private long _processedHours;

    public WorldClock(int startHour = WorldThresholds.DefaultStartHour)
    {
        if (startHour is < 0 or >= HoursPerDay)
            throw new ArgumentOutOfRangeException(nameof(startHour), startHour, "startHour must be 0..23");
        _totalMinutes = startHour * 60.0;
        _processedHours = startHour;
    }

    /// <summary>Disparado a cada hora cheia alcançada, com a hora do dia (0..23).</summary>
    public event Action<int>? HourElapsed;

    /// <summary>Disparado nas horas de <see cref="WorldThresholds.NormalizationHours"/>.</summary>
    public event Action? NormalizationDue;

    /// <summary>Disparado à meia-noite (antes do <see cref="HourElapsed"/> da hora 0).</summary>
    public event Action? DayElapsed;

    public int HourOfDay => (int)(_totalMinutes % 1440.0 / 60.0);
    public int MinuteOfHour => (int)(_totalMinutes % 60.0);
    public float MinuteOfDay => (float)(_totalMinutes % 1440.0);

    /// <summary>
    /// Avança o relógio. Aceita frações arbitrárias (frame a frame) e saltos
    /// grandes: cada hora cruzada dispara seus eventos, na ordem.
    /// </summary>
    public void Advance(float gameMinutes)
    {
        if (gameMinutes < 0f)
            throw new ArgumentOutOfRangeException(nameof(gameMinutes), gameMinutes, "gameMinutes must be >= 0");

        _totalMinutes += gameMinutes;
        long target = (long)(_totalMinutes / 60.0);

        while (_processedHours < target)
        {
            _processedHours++;
            int hour = (int)(_processedHours % HoursPerDay);

            if (hour == 0)
                DayElapsed?.Invoke();
            HourElapsed?.Invoke(hour);
            if (WorldThresholds.NormalizationHours.Contains(hour))
                NormalizationDue?.Invoke();
        }
    }
}
