namespace RelationshipSystem.Core;

/// <summary>
/// Um modificador nomeado no relacionamento (estilo Crusader Kings 3).
/// Exemplo: "+20 deu um presente" que expira em 48 horas.
/// </summary>
public sealed class RelationshipModifier
{
    private readonly string _name = string.Empty;

    public required string Name
    {
        get => _name;
        init => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Modifier name required", nameof(Name))
            : value;
    }

    public required float Value { get; init; }

    /// <summary>Tempo restante em horas.</summary>
    public float RemainingHours { get; set; }

    public bool IsExpired => RemainingHours <= 0f;

    /// <summary>
    /// Decrementa o tempo restante. Retorna true se expirou neste tick.
    /// </summary>
    public bool DecayTime(float hoursPassed)
    {
        if (hoursPassed < 0f)
            throw new ArgumentOutOfRangeException(nameof(hoursPassed), hoursPassed, "hoursPassed must be >= 0");

        RemainingHours -= hoursPassed;
        return IsExpired;
    }
}
