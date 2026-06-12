using Godot;

namespace BuildSystem;

/// <summary>Linhas-guia do grid isométrico. Visível só em construção.</summary>
public partial class BuildGridOverlay : Node2D
{
    [Export] public TileMapLayer Layer { get; set; } = null!;
    [Export] public int Radius { get; set; } = 12;

    private static readonly Color LineColor = new(1f, 1f, 1f, 0.18f);

    public override void _Ready()
    {
        if (Layer is null)
            throw new System.InvalidOperationException($"{nameof(BuildGridOverlay)} requires {nameof(Layer)}.");
    }

    public override void _Draw()
    {
        Vector2I ts = Layer.TileSet.TileSize;
        float hw = ts.X / 2f;
        float hh = ts.Y / 2f;

        for (int x = -Radius; x <= Radius; x++)
        {
            for (int y = -Radius; y <= Radius; y++)
            {
                Vector2 c = Layer.MapToLocal(new Vector2I(x, y));
                Vector2[] diamond =
                {
                    c + new Vector2(0, -hh),
                    c + new Vector2(hw, 0),
                    c + new Vector2(0, hh),
                    c + new Vector2(-hw, 0),
                    c + new Vector2(0, -hh),
                };
                DrawPolyline(diamond, LineColor, 1f);
            }
        }
    }
}
