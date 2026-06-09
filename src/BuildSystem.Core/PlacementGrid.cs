namespace BuildSystem.Core;

/// <summary>
/// Fonte de verdade da ocupação de OBJETOS no grid (o piso mora no TileMapLayer).
/// </summary>
public sealed class PlacementGrid
{
    private readonly Dictionary<GridCoord, Guid> _occupied = new();

    public bool CanPlace(IEnumerable<GridCoord> cells)
    {
        foreach (var c in cells)
            if (_occupied.ContainsKey(c))
                return false;
        return true;
    }

    /// <summary>Ocupa as células com o objeto. Lança se <see cref="CanPlace"/> for falso.</summary>
    public void Occupy(IEnumerable<GridCoord> cells, Guid objectId)
    {
        var list = cells as IReadOnlyCollection<GridCoord> ?? cells.ToList();
        if (!CanPlace(list))
            throw new InvalidOperationException("Cannot occupy: one or more cells are already taken.");
        foreach (var c in list)
            _occupied[c] = objectId;
    }

    public void Free(Guid objectId)
    {
        var keys = _occupied.Where(kv => kv.Value == objectId).Select(kv => kv.Key).ToList();
        foreach (var k in keys)
            _occupied.Remove(k);
    }

    public Guid? ObjectAt(GridCoord cell)
        => _occupied.TryGetValue(cell, out var id) ? id : null;
}
