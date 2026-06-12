namespace BuildSystem.Core;

/// <summary>Célula do grid isométrico. Coordenada própria, sem Godot.</summary>
public readonly record struct GridCoord(int X, int Y)
{
    public static GridCoord operator +(GridCoord a, GridCoord b) => new(a.X + b.X, a.Y + b.Y);
}
