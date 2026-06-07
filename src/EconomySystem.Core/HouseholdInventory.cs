namespace EconomySystem.Core;

/// <summary>
/// Inventário de bens de um domicílio: Id → <see cref="OwnedObject"/>.
/// Espelha o padrão de store do <c>RelationshipMatrix</c> (dicionário +
/// Get/TryGet/All/Count). O valor total alimenta as contas e o patrimônio.
/// </summary>
public sealed class HouseholdInventory
{
    private readonly Dictionary<string, OwnedObject> _objects = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Adiciona (ou substitui) um objeto.</summary>
    public void Add(OwnedObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        _objects[obj.Id] = obj;
    }

    /// <summary>Remove um objeto. Retorna true se existia.</summary>
    public bool Remove(string id) => _objects.Remove(id);

    public bool TryGet(string id, out OwnedObject? obj) => _objects.TryGetValue(id, out obj);

    /// <summary>Todos os objetos.</summary>
    public IEnumerable<OwnedObject> All => _objects.Values;

    public int Count => _objects.Count;

    /// <summary>Soma do valor atual de TODOS os objetos (para patrimônio líquido).</summary>
    public int TotalObjectValue => _objects.Values.Sum(o => o.CurrentValue);

    /// <summary>Soma só dos objetos faturáveis (base do cálculo da conta).</summary>
    public int BillableObjectValue => _objects.Values.Where(o => o.Billable).Sum(o => o.CurrentValue);
}
