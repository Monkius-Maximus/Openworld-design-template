using System;
using System.Collections.Generic;
using System.Linq;
using BuildSystem.Core;
using Godot;
using MovementSystem.Core;

namespace BuildSystem.Game;

/// <summary>
/// Porte do modo construção para o mundo 3D reutilizando o MESMO núcleo do
/// modo 2D (PlacementGrid/Footprint/PlacementRotation), agora sobre células
/// de 1 m no plano XZ. O cursor usa o raio×plano do AimCalculator (o mesmo da
/// mira do personagem). Objetos colocados têm colisão real — o jogador
/// colide com o que construir. B alterna, R gira, X demole, clique confirma.
/// </summary>
public partial class BuildController3D : Node
{
    [Export] public Node3D ObjectsRoot { get; set; } = null!;

    /// <summary>Contêiner (no HUD) que recebe os botões do catálogo.</summary>
    [Export] public Control Palette { get; set; } = null!;

    public bool BuildModeActive { get; private set; }

    /// <summary>Desligado pelo GameManager enquanto menus estão abertos.</summary>
    public bool InputEnabled { get; set; } = true;

    private sealed record BuildItem(string Id, string DisplayName, Vector2I Footprint, Color Color, float Height);

    private static readonly BuildItem[] Catalog =
    {
        new("mesa", "Mesa (2x1)", new Vector2I(2, 1), new Color(0.55f, 0.36f, 0.2f), 0.75f),
        new("cadeira", "Cadeira", new Vector2I(1, 1), new Color(0.85f, 0.55f, 0.25f), 0.45f),
        new("caixa", "Caixa", new Vector2I(1, 1), new Color(0.6f, 0.6f, 0.62f), 1.0f),
    };

    private readonly PlacementGrid _grid = new();
    private readonly Dictionary<Guid, Node3D> _objects = new();

    private BuildItem? _active;
    private Footprint? _footprint;
    private PlacementRotation _rotation = PlacementRotation.Deg0;
    private bool _demolish;

    private MeshInstance3D _cursor = null!;
    private StandardMaterial3D _cursorMaterial = null!;
    private GridCoord _hoveredCell;
    private bool _hasHover;

    public override void _Ready()
    {
        if (ObjectsRoot is null) throw new InvalidOperationException($"{nameof(BuildController3D)} requires {nameof(ObjectsRoot)}.");
        if (Palette is null) throw new InvalidOperationException($"{nameof(BuildController3D)} requires {nameof(Palette)}.");

        BuildPaletteButtons();
        CreateCursor();
        SetBuildMode(false);
    }

    private void BuildPaletteButtons()
    {
        Palette.AddChild(new Label { Text = "Construção — R gira, X demole" });
        foreach (BuildItem item in Catalog)
        {
            BuildItem captured = item;
            var button = new Button { Text = captured.DisplayName };
            button.Pressed += () => SelectItem(captured);
            Palette.AddChild(button);
        }
    }

    private void CreateCursor()
    {
        _cursorMaterial = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = new Color(1f, 1f, 1f, 0.3f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        _cursor = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(1f, 0.08f, 1f), Material = _cursorMaterial },
            Visible = false,
        };
        ObjectsRoot.AddChild(_cursor);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!InputEnabled)
            return;

        if (@event.IsActionPressed("build_toggle"))
        {
            SetBuildMode(!BuildModeActive);
            return;
        }

        if (!BuildModeActive)
            return;

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
                Confirm();
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                _active = null;
                _footprint = null;
                _demolish = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!BuildModeActive)
            return;

        _hasHover = TryProjectMouseToCell(out _hoveredCell);
        UpdateCursor();
    }

    private void SetBuildMode(bool on)
    {
        BuildModeActive = on;
        Palette.Visible = on;
        _cursor.Visible = false;

        if (!on)
        {
            _active = null;
            _footprint = null;
            _demolish = false;
        }
    }

    private void SelectItem(BuildItem item)
    {
        _active = item;
        _demolish = false;
        _footprint = Footprint.Rectangle(Math.Max(1, item.Footprint.X), Math.Max(1, item.Footprint.Y));
    }

    /// <summary>Célula de 1 m no plano XZ sob o cursor (raio×plano, como a mira).</summary>
    private bool TryProjectMouseToCell(out GridCoord cell)
    {
        cell = default;
        Camera3D? camera = GetViewport().GetCamera3D();
        if (camera is null)
            return false;

        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 origin = camera.ProjectRayOrigin(mouse);
        Vector3 direction = camera.ProjectRayNormal(mouse);

        if (!AimCalculator.TryIntersectPlane(
                new System.Numerics.Vector3(origin.X, origin.Y, origin.Z),
                new System.Numerics.Vector3(direction.X, direction.Y, direction.Z),
                0f, out System.Numerics.Vector3 hit))
            return false;

        cell = new GridCoord((int)MathF.Floor(hit.X), (int)MathF.Floor(hit.Z));
        return true;
    }

    private void UpdateCursor()
    {
        if (!_hasHover)
        {
            _cursor.Visible = false;
            return;
        }

        List<GridCoord> cells = _demolish || _footprint is null
            ? new List<GridCoord> { _hoveredCell }
            : _footprint.CellsAt(_hoveredCell, _rotation).ToList();

        (Vector3 center, Vector3 size) = BoundsOf(cells, 0.08f);
        _cursor.Position = new Vector3(center.X, 0.05f, center.Z);
        _cursor.Scale = new Vector3(size.X, 1f, size.Z);
        _cursor.Visible = true;

        bool valid = !_demolish && _footprint is not null && _grid.CanPlace(cells);
        _cursorMaterial.AlbedoColor = _demolish
            ? new Color(0.9f, 0.2f, 0.2f, 0.45f)
            : _footprint is null
                ? new Color(1f, 1f, 1f, 0.25f)
                : valid
                    ? new Color(0.2f, 0.9f, 0.3f, 0.45f)
                    : new Color(0.9f, 0.2f, 0.2f, 0.45f);
    }

    private void Confirm()
    {
        if (!_hasHover)
            return;

        if (_demolish)
        {
            Demolish(_hoveredCell);
            return;
        }

        if (_active is null || _footprint is null)
            return;

        List<GridCoord> cells = _footprint.CellsAt(_hoveredCell, _rotation).ToList();
        if (!_grid.CanPlace(cells))
            return;

        var id = Guid.NewGuid();
        Node3D node = CreatePlacedNode(_active, cells);
        ObjectsRoot.AddChild(node);
        _grid.Occupy(cells, id);
        _objects[id] = node;
    }

    private void Demolish(GridCoord cell)
    {
        Guid? id = _grid.ObjectAt(cell);
        if (id is not { } objectId)
            return;

        _grid.Free(objectId);
        _objects[objectId].QueueFree();
        _objects.Remove(objectId);
    }

    /// <summary>Corpo estático com malha e colisão cobrindo as células (o player colide).</summary>
    private static Node3D CreatePlacedNode(BuildItem item, List<GridCoord> cells)
    {
        (Vector3 center, Vector3 size) = BoundsOf(cells, item.Height);
        var meshSize = new Vector3(size.X * 0.92f, item.Height, size.Z * 0.92f);

        var body = new StaticBody3D { Position = center };
        body.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = meshSize,
                Material = new StandardMaterial3D { AlbedoColor = item.Color },
            },
            Position = new Vector3(0f, item.Height / 2f, 0f),
        });
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = meshSize },
            Position = new Vector3(0f, item.Height / 2f, 0f),
        });
        return body;
    }

    /// <summary>Centro e extensão (em células → metros) do retângulo ocupado.</summary>
    private static (Vector3 Center, Vector3 Size) BoundsOf(IReadOnlyCollection<GridCoord> cells, float height)
    {
        int minX = cells.Min(c => c.X);
        int maxX = cells.Max(c => c.X);
        int minZ = cells.Min(c => c.Y);
        int maxZ = cells.Max(c => c.Y);

        int width = maxX - minX + 1;
        int depth = maxZ - minZ + 1;
        var center = new Vector3(minX + width / 2f, 0f, minZ + depth / 2f);
        return (center, new Vector3(width, height, depth));
    }
}
