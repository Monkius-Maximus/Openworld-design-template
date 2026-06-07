namespace RelationshipSystem.Core;

/// <summary>
/// Registro de personagens por Id. Dá ao resolver acesso aos
/// <see cref="CharacterTraits"/> (necessário, por exemplo, para modular
/// conversas por interesses compartilhados). É opcional: sem ele, o sistema
/// continua funcionando, apenas ignorando os tópicos de conversa.
/// </summary>
public sealed class CharacterRegistry
{
    private readonly Dictionary<string, CharacterTraits> _byId =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registra (ou substitui) um personagem pelo seu próprio Id.</summary>
    public void Add(CharacterTraits character)
    {
        ArgumentNullException.ThrowIfNull(character);
        _byId[character.Id] = character;
    }

    public bool TryGet(string id, out CharacterTraits character)
    {
        if (id is not null && _byId.TryGetValue(id, out var found))
        {
            character = found;
            return true;
        }
        character = null!;
        return false;
    }

    public CharacterTraits Get(string id) =>
        TryGet(id, out var c)
            ? c
            : throw new KeyNotFoundException($"Personagem '{id}' não registrado.");

    public int Count => _byId.Count;
}
