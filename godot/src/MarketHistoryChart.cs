using System.Collections.Generic;
using EconomySystem.Core.Market;
using Godot;

namespace BuildSystem;

/// <summary>
/// Gráfico simples (v6) do índice global de inflação ao longo do tempo,
/// construído em código (sem cena). Plota a série de <see cref="MarketHistory"/>
/// como uma polilinha normalizada à altura do controle, com uma linha de base
/// em índice = 1.0 (sem deriva). Chamar <see cref="SetHistory"/> e redesenha.
/// </summary>
public partial class MarketHistoryChart : Control
{
    private readonly List<MarketSample> _samples = new();

    public MarketHistoryChart()
    {
        CustomMinimumSize = new Vector2(360, 120);
    }

    /// <summary>Substitui a série exibida e força o redesenho.</summary>
    public void SetHistory(IReadOnlyList<MarketSample> samples)
    {
        _samples.Clear();
        _samples.AddRange(samples);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var size = Size;
        DrawRect(new Rect2(Vector2.Zero, size), new Color(0.12f, 0.12f, 0.14f));

        if (_samples.Count < 2)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(8, 20),
                "Sem histórico — avance alguns dias.", HorizontalAlignment.Left, -1, 14);
            return;
        }

        // Faixa de valores, com a base 1.0 sempre visível.
        decimal min = 1m, max = 1m;
        foreach (var s in _samples)
        {
            if (s.GlobalIndex < min) min = s.GlobalIndex;
            if (s.GlobalIndex > max) max = s.GlobalIndex;
        }
        if (max - min < 0.0001m) max = min + 0.0001m;

        float X(int i) => size.X * i / (_samples.Count - 1);
        float Y(decimal v) => size.Y * (1f - (float)((v - min) / (max - min)));

        // Linha de base (índice = 1.0).
        if (min <= 1m && 1m <= max)
        {
            float yBase = Y(1m);
            DrawLine(new Vector2(0, yBase), new Vector2(size.X, yBase),
                new Color(0.4f, 0.4f, 0.45f), 1f);
        }

        var points = new Vector2[_samples.Count];
        for (int i = 0; i < _samples.Count; i++)
            points[i] = new Vector2(X(i), Y(_samples[i].GlobalIndex));
        DrawPolyline(points, new Color(0.3f, 0.8f, 0.4f), 2f, true);

        DrawString(ThemeDB.FallbackFont, new Vector2(8, 16),
            $"índice global {min:0.000}–{max:0.000} (dias {_samples[0].Day}–{_samples[^1].Day})",
            HorizontalAlignment.Left, -1, 13, new Color(0.8f, 0.8f, 0.8f));
    }
}
