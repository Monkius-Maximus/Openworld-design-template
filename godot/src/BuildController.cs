using System;
using System.Collections.Generic;
using System.Linq;
using BuildSystem.Core;
using Godot;

namespace BuildSystem;

public partial class BuildController : Node
{
    [Export] public TileMapLayer FloorLayer { get; set; } = null!;
    [Export] public BuildCursor Cursor { get; set; } = null!;
    [Export] public BuildGridOverlay Overlay { get; set; } = null!;
    [Export] public Node ObjectsRoot { get; set; } = null!;
    [Export] public Player Player { get; set; } = null!;
    [Export] public Godot.Collections.Array<Placeable> Catalog { get; set; } = null!;
    [Export] public Control Palette { get; set; } = null!;
    [Export] public int FloorSourceId { get; set; }

    private readonly PlacementGrid _grid = new();
    private readonly Dictionary<Guid, PlacedObject> _objects = new();

    private bool _buildMode;
    private bool _demolish;
    private Placeable? _active;
    private Footprint? _footprint;
    private PlacementRotation _rotation = PlacementRotation.Deg0;

    public override void _Ready()
    {
        if (FloorLayer is null) throw new InvalidOperationException($"{nameof(BuildController)} requires {nameof(FloorLayer)}.");
        if (Cursor is null) throw new InvalidOperationException($"{nameof(BuildController)} requires {nameof(Cursor)}.");
        if (Overlay is null) throw new InvalidOperationException($"{nameof(BuildController)} requires {nameof(Overlay)}.");
        if (ObjectsRoot is null) throw new InvalidOperationException($"{nameof(BuildController)} requires {nameof(ObjectsRoot)}.");
        if (Player is null) throw new InvalidOperationException($"{nameof(BuildController)} requires {nameof(Player)}.");
        if (Catalog is null) throw new InvalidOperationException($"{nameof(BuildController)} requires {nameof(Catalog)}.");
        if (Palette is null) throw new InvalidOperationException($"{nameof(BuildController)} requires {nameof(Palette)}.");

        BuildPaletteButtons();
        SetBuildMode(false);
    }

    private void BuildPaletteButtons()
    {
        foreach (Placeable p in Catalog)
        {
            var button = new Button { Text = string.IsNullOrEmpty(p.DisplayName) ? p.Id : p.DisplayName };
            button.Connect(BaseButton.SignalName.Pressed, Callable.From(() => SelectPlaceable(p)));
            Palette.AddChild(button);
        }
    }

    public override void _Process(double delta)
    {
        if (!_buildMode) return;
        Vector2I cell = HoveredCell();
        Cursor.GlobalPosition = FloorLayer.ToGlobal(FloorLayer.MapToLocal(cell));
        UpdatePreview(cell);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("build_toggle"))
        {
            SetBuildMode(!_buildMode);
            return;
        }

        if (!_buildMode) return;

        if (@event.IsActionPressed("build_rotate"))
        {
            _rotation = (PlacementRotation)(((int)_rotation + 1) % 4);
            return;
        }

        if (@event.IsActionPressed("build_demolish"))
        {
            _demolish = !_demolish;
            return;
        }

        if (@event is InputEventMouseButton { Pressed: true } mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
                Confirm(HoveredCell());
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                _active = null;
                _footprint = null;
                _demolish = false;
            }
        }
    }

    private void SetBuildMode(bool on)
    {
        _buildMode = on;
        Player.ControlEnabled = !on;
        Cursor.Visible = on;
        Overlay.Visible = on;
        Palette.Visible = on;

        if (on)
        {
            Overlay.QueueRedraw();
        }
        else
        {
            _active = null;
            _footprint = null;
            _demolish = false;
        }
    }

    private void SelectPlaceable(Placeable p)
    {
        _active = p;
        _demolish = false;
        _footprint = Footprint.Rectangle(Math.Max(1, p.FootprintSize.X), Math.Max(1, p.FootprintSize.Y));
    }

    private void UpdatePreview(Vector2I cell)
    {
        if (_demolish)
        {
            Cursor.SetCells(Diamonds(cell, new[] { ToCoord(cell) }));
            Cursor.SetValid(false);
            return;
        }

        if (_active is null)
        {
            Cursor.SetCells(System.Array.Empty<Vector2[]>());
            return;
        }

        if (_active.Category == PlaceableCategory.Floor)
        {
            Cursor.SetCells(Diamonds(cell, new[] { ToCoord(cell) }));
            Cursor.SetValid(true);
            return;
        }

        List<GridCoord> cells = _footprint!.CellsAt(ToCoord(cell), _rotation).ToList();
        Cursor.SetCells(Diamonds(cell, cells));
        Cursor.SetValid(_grid.CanPlace(cells));
    }

    private void Confirm(Vector2I cell)
    {
        if (_demolish)
        {
            Demolish(cell);
            return;
        }

        if (_active is null) return;

        if (_active.Category == PlaceableCategory.Floor)
        {
            FloorLayer.SetCell(cell, FloorSourceId, _active.AtlasCoords);
            return;
        }

        List<GridCoord> cells = _footprint!.CellsAt(ToCoord(cell), _rotation).ToList();
        if (!_grid.CanPlace(cells)) return;

        PackedScene scene = _active.Scene
            ?? throw new InvalidOperationException($"Placeable '{_active.Id}' is an Object but has no Scene.");
        var node = scene.Instantiate<PlacedObject>();
        ObjectsRoot.AddChild(node);
        node.GlobalPosition = FloorLayer.ToGlobal(FloorLayer.MapToLocal(cell));
        node.RotationDegrees = (int)_rotation * 90;

        var id = Guid.NewGuid();
        _grid.Occupy(cells, id);
        _objects[id] = node;
    }

    private void Demolish(Vector2I cell)
    {
        Guid? id = _grid.ObjectAt(ToCoord(cell));
        if (id is { } objectId)
        {
            _grid.Free(objectId);
            _objects[objectId].QueueFree();
            _objects.Remove(objectId);
            return;
        }

        if (FloorLayer.GetCellSourceId(cell) != -1)
            FloorLayer.EraseCell(cell);
    }

    private Vector2I HoveredCell() => FloorLayer.LocalToMap(FloorLayer.GetLocalMousePosition());

    private Vector2[][] Diamonds(Vector2I originCell, IEnumerable<GridCoord> cells)
    {
        Vector2I ts = FloorLayer.TileSet.TileSize;
        float hw = ts.X / 2f;
        float hh = ts.Y / 2f;
        Vector2 originLocal = FloorLayer.MapToLocal(originCell);

        var result = new List<Vector2[]>();
        foreach (GridCoord gc in cells)
        {
            Vector2 c = FloorLayer.MapToLocal(ToVec(gc)) - originLocal;
            result.Add(new[]
            {
                c + new Vector2(0, -hh),
                c + new Vector2(hw, 0),
                c + new Vector2(0, hh),
                c + new Vector2(-hw, 0),
            });
        }
        return result.ToArray();
    }

    private static GridCoord ToCoord(Vector2I v) => new(v.X, v.Y);
    private static Vector2I ToVec(GridCoord c) => new(c.X, c.Y);
}
