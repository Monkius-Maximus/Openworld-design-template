using Godot;

namespace BuildSystem;

/// <summary>Fantasma de preview. Verde = válido, vermelho = inválido.</summary>
public partial class BuildCursor : Node2D
{
    private static readonly Color Green = new(0.3f, 1f, 0.3f, 0.9f);
    private static readonly Color Red = new(1f, 0.3f, 0.3f, 0.9f);

    private Color _color = Green;
    private Vector2[][] _cells = System.Array.Empty<Vector2[]>();

    public void SetValid(bool valid)
    {
        _color = valid ? Green : Red;
        QueueRedraw();
    }

    /// <summary>Diamantes (em espaço local) que formam o contorno do footprint.</summary>
    public void SetCells(Vector2[][] cellDiamonds)
    {
        _cells = cellDiamonds;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var fill = new Color(_color, 0.25f);
        foreach (var diamond in _cells)
        {
            DrawColoredPolygon(diamond, fill);
            DrawPolyline(Close(diamond), _color, 2f);
        }
    }

    private static Vector2[] Close(Vector2[] diamond)
    {
        var loop = new Vector2[diamond.Length + 1];
        diamond.CopyTo(loop, 0);
        loop[^1] = diamond[0];
        return loop;
    }
}
