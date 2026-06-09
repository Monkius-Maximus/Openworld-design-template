using BuildSystem.Core;
using Xunit;

namespace BuildSystem.Tests;

public class PlacementGridTests
{
    [Fact]
    public void CanPlace_rejects_overlap()
    {
        var grid = new PlacementGrid();
        var footprint = Footprint.Rectangle(2, 1);
        var a = footprint.CellsAt(new GridCoord(0, 0), PlacementRotation.Deg0);

        grid.Occupy(a, Guid.NewGuid());

        // (1,0) é ocupada por A; um objeto em (1,0) sobrepõe.
        var b = footprint.CellsAt(new GridCoord(1, 0), PlacementRotation.Deg0);
        Assert.False(grid.CanPlace(b));
    }

    [Fact]
    public void Footprint_rotation_changes_occupied_cells()
    {
        var footprint = Footprint.Rectangle(2, 1);

        var flat = footprint.CellsAt(new GridCoord(0, 0), PlacementRotation.Deg0).ToHashSet();
        var turned = footprint.CellsAt(new GridCoord(0, 0), PlacementRotation.Deg90).ToHashSet();

        Assert.Equal(new HashSet<GridCoord> { new(0, 0), new(1, 0) }, flat);
        Assert.Equal(new HashSet<GridCoord> { new(0, 0), new(0, 1) }, turned);
    }

    [Fact]
    public void Free_releases_cells_for_reuse()
    {
        var grid = new PlacementGrid();
        var footprint = Footprint.Rectangle(1, 1);
        var id = Guid.NewGuid();
        var cells = footprint.CellsAt(new GridCoord(3, 4), PlacementRotation.Deg0).ToList();

        grid.Occupy(cells, id);
        Assert.False(grid.CanPlace(cells));

        grid.Free(id);

        Assert.True(grid.CanPlace(cells));
        Assert.Null(grid.ObjectAt(new GridCoord(3, 4)));
    }

    [Fact]
    public void Occupy_throws_when_cannot_place()
    {
        var grid = new PlacementGrid();
        var cell = new[] { new GridCoord(0, 0) };
        grid.Occupy(cell, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => grid.Occupy(cell, Guid.NewGuid()));
    }
}
