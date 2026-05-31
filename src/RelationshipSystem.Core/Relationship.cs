namespace RelationshipSystem.Core;

public enum RelationshipFlag { Friend, BestFriend, Enemy, Crush, Love }

/// <summary>
/// Relacionamento de uma direção: A → B.
/// A assimetria é intencional: A→B pode diferir de B→A
/// (paixão não-correspondida, mágoa unilateral).
/// </summary>
public sealed class Relationship
{
    /// <summary>Nome do modificador de fúria gerado por <c>Insult</c>.</summary>
    public const string FuryModifierName = "Foi insultado";

    private readonly string _fromId = string.Empty;
    private readonly string _toId = string.Empty;

    public required string FromId
    {
        get => _fromId;
        init => _fromId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("FromId required", nameof(FromId))
            : value;
    }

    public required string ToId
    {
        get => _toId;
        init => _toId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("ToId required", nameof(ToId))
            : value;
    }

    /// <summary>Valores: daily e lifetime.</summary>
    public RelationshipValue Value { get; } = new();

    /// <summary>Score de atração (separado, eixo diferente).</summary>
    public int AttractionScore { get; set; } = 0;

    /// <summary>Flags de relacionamento (Friend, Enemy, Love, etc).</summary>
    public HashSet<RelationshipFlag> Flags { get; } = new();

    /// <summary>Modificadores nomeados (turnos, presentes, ofensas, etc).</summary>
    public List<RelationshipModifier> Modifiers { get; } = new();

    /// <summary>Score efetivo = Value + soma dos modificadores (ignora expirados).</summary>
    public float EffectiveDaily => Value.Daily + ModifierSum();

    public float EffectiveLifetime => Value.Lifetime + ModifierSum();

    /// <summary>True enquanto houver um modificador de fúria ativo.</summary>
    public bool IsFurious =>
        Modifiers.Any(m => !m.IsExpired && m.Name == FuryModifierName);

    private float ModifierSum() =>
        Modifiers.Where(m => !m.IsExpired).Sum(m => m.Value);

    /// <summary>Decai modificadores; remove os expirados.</summary>
    public void DecayModifiers(float hoursPassed)
    {
        Modifiers.RemoveAll(m => m.DecayTime(hoursPassed));
    }

    /// <summary>Adiciona um modificador (ex: "foi ofendido").</summary>
    public void AddModifier(RelationshipModifier mod)
    {
        ArgumentNullException.ThrowIfNull(mod);
        Modifiers.Add(mod);
    }
}
