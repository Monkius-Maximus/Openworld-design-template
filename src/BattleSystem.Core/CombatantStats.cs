namespace BattleSystem.Core;

/// <summary>
/// Atributos imutáveis de um combatente. Validação fail-fast nos setters
/// <c>init</c> (mesma decisão do <c>CharacterTraits</c>: o corpo do construtor
/// roda antes dos inicializadores, então a validação vive nos <c>init</c>).
/// </summary>
public sealed class CombatantStats
{
    private readonly string _id = string.Empty;
    private readonly float _maxHealth;
    private readonly float _maxStamina;
    private readonly float _attack;
    private readonly float _defense;
    private readonly float _speed;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Id required", nameof(Id))
            : value;
    }

    public required float MaxHealth
    {
        get => _maxHealth;
        init => _maxHealth = value > 0f
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaxHealth), value, "MaxHealth must be > 0");
    }

    public required float MaxStamina
    {
        get => _maxStamina;
        init => _maxStamina = value > 0f
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaxStamina), value, "MaxStamina must be > 0");
    }

    public required float Attack
    {
        get => _attack;
        init => _attack = value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Attack), value, "Attack must be >= 0");
    }

    public required float Defense
    {
        get => _defense;
        init => _defense = value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Defense), value, "Defense must be >= 0");
    }

    /// <summary>Define a ordem dos turnos (maior age primeiro).</summary>
    public required float Speed
    {
        get => _speed;
        init => _speed = value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Speed), value, "Speed must be >= 0");
    }
}
