namespace RelationshipSystem.Core;

public enum Zodiac
{
    Aries, Taurus, Gemini, Cancer, Leo, Virgo,
    Libra, Scorpio, Sagittarius, Capricorn, Aquarius, Pisces
}

public enum Aspiration { Family, Wealth, Popularity, Knowledge, Romance, Pleasure }

/// <summary>
/// Personalidade no estilo The Sims 2. Cada eixo vai de 0 a 10.
/// A validação é fail-fast: valores fora de 0..10 lançam imediatamente.
/// </summary>
public readonly record struct Personality(
    int Neat,     // 0..10
    int Outgoing, // 0..10
    int Active,   // 0..10
    int Playful,  // 0..10
    int Nice      // 0..10
)
{
    public int Neat { get; init; } = Validate(Neat, nameof(Neat));
    public int Outgoing { get; init; } = Validate(Outgoing, nameof(Outgoing));
    public int Active { get; init; } = Validate(Active, nameof(Active));
    public int Playful { get; init; } = Validate(Playful, nameof(Playful));
    public int Nice { get; init; } = Validate(Nice, nameof(Nice));

    private static int Validate(int value, string name) =>
        value is >= 0 and <= 10
            ? value
            : throw new ArgumentOutOfRangeException(name, value, "Personality values must be 0..10");
}

/// <summary>
/// Traços imutáveis de um personagem. A validação acontece nos setters
/// <c>init</c> (fail-fast no momento da construção via object initializer).
/// </summary>
public sealed class CharacterTraits
{
    private readonly string _id = string.Empty;
    private readonly string _name = string.Empty;
    private readonly IReadOnlyList<string> _turnOns = Array.Empty<string>();
    private readonly string _turnOff = string.Empty;
    private readonly IReadOnlyList<string> _tags = Array.Empty<string>();

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Id required", nameof(Id))
            : value;
    }

    public required string Name
    {
        get => _name;
        init => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Name required", nameof(Name))
            : value;
    }

    public required Zodiac Zodiac { get; init; }
    public required Aspiration Aspiration { get; init; }
    public required Personality Personality { get; init; }

    /// <summary>Exatamente 2 turn-ons (strings livres).</summary>
    public required IReadOnlyList<string> TurnOns
    {
        get => _turnOns;
        init
        {
            if (value is null || value.Count != 2)
                throw new ArgumentException("Exactly 2 turn-ons required", nameof(TurnOns));
            if (value.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Turn-ons cannot be blank", nameof(TurnOns));
            _turnOns = value.ToArray();
        }
    }

    /// <summary>Exatamente 1 turn-off.</summary>
    public required string TurnOff
    {
        get => _turnOff;
        init => _turnOff = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Turn-off required", nameof(TurnOff))
            : value;
    }

    /// <summary>Tags que descrevem este personagem (ex: "Cologne", "Fitness", "Blond", "Shy").</summary>
    public required IReadOnlyList<string> Tags
    {
        get => _tags;
        init
        {
            if (value is null)
                throw new ArgumentException("Tags required", nameof(Tags));
            if (value.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Tags cannot be blank", nameof(Tags));
            _tags = value.ToArray();
        }
    }
}
