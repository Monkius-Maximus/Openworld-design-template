namespace BuildSystem.Core;

/// <summary>
/// Conjunto de células relativas ocupadas por um placeable (não um W×H).
/// <see cref="CellsAt"/> projeta o footprint numa origem e rotação concretas.
/// </summary>
public sealed class Footprint
{
    private readonly IReadOnlyCollection<GridCoord> _cells;

    public Footprint(IEnumerable<GridCoord> relativeCells)
    {
        var set = new HashSet<GridCoord>(relativeCells);
        if (set.Count == 0)
            throw new ArgumentException("Footprint requires at least one cell.", nameof(relativeCells));
        _cells = set;
    }

    /// <summary>Footprint retangular a partir de um tamanho W×H, ancorado em (0,0).</summary>
    public static Footprint Rectangle(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "width must be > 0");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "height must be > 0");

        var cells = new List<GridCoord>(width * height);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cells.Add(new GridCoord(x, y));
        return new Footprint(cells);
    }

    public IEnumerable<GridCoord> CellsAt(GridCoord origin, PlacementRotation rotation)
    {
        foreach (var c in _cells)
            yield return origin + Rotate(c, rotation);
    }

    private static GridCoord Rotate(GridCoord c, PlacementRotation rotation) => rotation switch
    {
        PlacementRotation.Deg0 => c,
        PlacementRotation.Deg90 => new GridCoord(-c.Y, c.X),
        PlacementRotation.Deg180 => new GridCoord(-c.X, -c.Y),
        PlacementRotation.Deg270 => new GridCoord(c.Y, -c.X),
        _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
    };
}
