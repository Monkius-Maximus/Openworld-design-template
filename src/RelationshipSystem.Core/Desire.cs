namespace RelationshipSystem.Core;

/// <summary>Um <see cref="Desire"/> é um anseio (Want) ou um pavor (Fear).</summary>
public enum DesireKind { Want, Fear }

/// <summary>
/// Desejo dinâmico ligado a um relacionamento, estilo The Sims 2. Cumprir um
/// Want enche a aspiração; realizar um Fear a drena. A condição é avaliada
/// sobre a matriz inteira (dono → alvo, e o inverso, se a condição quiser),
/// permitindo medos como "não ser correspondido".
/// </summary>
public sealed class Desire
{
    private readonly string _id = string.Empty;
    private readonly string _description = string.Empty;
    private readonly string _targetId = string.Empty;
    private readonly int _aspirationValue;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Id required", nameof(Id))
            : value;
    }

    public required string Description
    {
        get => _description;
        init => _description = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Description required", nameof(Description))
            : value;
    }

    public required DesireKind Kind { get; init; }

    /// <summary>Sobre quem é o desejo.</summary>
    public required string TargetId
    {
        get => _targetId;
        init => _targetId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("TargetId required", nameof(TargetId))
            : value;
    }

    /// <summary>Magnitude da aspiração (sempre &gt; 0); soma se Want, subtrai se Fear.</summary>
    public required int AspirationValue
    {
        get => _aspirationValue;
        init => _aspirationValue = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(AspirationValue), value, "AspirationValue must be > 0");
    }

    /// <summary>
    /// Condição de realização: <c>(matrix, ownerId, targetId) =&gt; bool</c>.
    /// </summary>
    public required Func<RelationshipMatrix, string, string, bool> IsMet { get; init; }
}
