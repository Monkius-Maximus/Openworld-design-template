using Godot;

namespace BuildSystem;

public enum PlaceableCategory { Floor, Object }

[GlobalClass]
public partial class Placeable : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public PlaceableCategory Category { get; set; }

    /// <summary>Cena instanciada quando <see cref="Category"/> é Object.</summary>
    [Export] public PackedScene? Scene { get; set; }

    /// <summary>Atlas do tile pintado quando <see cref="Category"/> é Floor.</summary>
    [Export] public Vector2I AtlasCoords { get; set; }

    [Export] public Vector2I FootprintSize { get; set; } = Vector2I.One;
}
