namespace RelationshipSystem.Core;

/// <summary>
/// Armazena todos os relacionamentos do jogo: (FromId, ToId) → Relationship.
/// Como cada direção é uma entrada distinta, a assimetria A→B ≠ B→A é nativa.
/// </summary>
public sealed class RelationshipMatrix
{
    private readonly Dictionary<(string From, string To), Relationship> _relationships = new();

    /// <summary>Obtém ou cria o relacionamento A → B.</summary>
    public Relationship Get(string fromId, string toId)
    {
        if (string.IsNullOrWhiteSpace(fromId))
            throw new ArgumentException("fromId required", nameof(fromId));
        if (string.IsNullOrWhiteSpace(toId))
            throw new ArgumentException("toId required", nameof(toId));
        if (fromId == toId)
            throw new ArgumentException("A relationship requires two distinct characters", nameof(toId));

        var key = (fromId, toId);
        if (!_relationships.TryGetValue(key, out var rel))
        {
            rel = new Relationship { FromId = fromId, ToId = toId };
            _relationships[key] = rel;
        }
        return rel;
    }

    /// <summary>Obtém o relacionamento se já existir, sem criá-lo.</summary>
    public bool TryGet(string fromId, string toId, out Relationship? relationship) =>
        _relationships.TryGetValue((fromId, toId), out relationship);

    /// <summary>Todos os relacionamentos.</summary>
    public IEnumerable<Relationship> All => _relationships.Values;

    /// <summary>Quantidade de direções registradas.</summary>
    public int Count => _relationships.Count;

    /// <summary>Amizade é MÚTUA: ambos daily &gt;= threshold.</summary>
    public bool AreFriends(string a, string b) =>
        Get(a, b).EffectiveDaily >= RelationshipThresholds.Friend &&
        Get(b, a).EffectiveDaily >= RelationshipThresholds.Friend;

    /// <summary>Melhor amizade é MÚTUA: ambos lifetime &gt;= threshold.</summary>
    public bool AreBestFriends(string a, string b) =>
        Get(a, b).EffectiveLifetime >= RelationshipThresholds.BestFriend &&
        Get(b, a).EffectiveLifetime >= RelationshipThresholds.BestFriend;

    /// <summary>Inimigos: ao menos um lado com daily &lt;= threshold.</summary>
    public bool AreEnemies(string a, string b) =>
        Get(a, b).EffectiveDaily <= RelationshipThresholds.Enemy ||
        Get(b, a).EffectiveDaily <= RelationshipThresholds.Enemy;
}
