using System;
using BuildSystem.Core;
using Godot;

namespace BuildSystem;

/// <summary>
/// Raiz da cena de um objeto colocado. Só visual no MVP.
/// A arte real entra como 4 texturas direcionais (uma por <see cref="PlacementRotation"/>)
/// no Sprite2D — origem no centro da BASE, pra Y-sort e snap à célula baterem.
/// </summary>
public partial class PlacedObject : Node2D
{
    [Export] public Sprite2D Sprite { get; set; } = null!;

    /// <summary>Uma textura por direção, na ordem Deg0, Deg90, Deg180, Deg270.</summary>
    [Export] public Godot.Collections.Array<Texture2D> DirectionTextures { get; set; } = null!;

    public override void _Ready()
    {
        if (Sprite is null)
            throw new InvalidOperationException($"{nameof(PlacedObject)} requires {nameof(Sprite)}.");
        if (DirectionTextures is null || DirectionTextures.Count != 4)
            throw new InvalidOperationException(
                $"{nameof(PlacedObject)} requires exactly 4 {nameof(DirectionTextures)} (Deg0, Deg90, Deg180, Deg270).");
    }

    /// <summary>Troca a textura pela direção. Não gira o nó — girar quebra a perspectiva iso.</summary>
    public void ApplyRotation(PlacementRotation rotation)
    {
        Sprite.Texture = DirectionTextures[(int)rotation];
    }
}
