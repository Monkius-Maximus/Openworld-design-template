using System;
using System.Collections.Generic;
using Godot;

namespace BuildSystem.Game;

/// <summary>
/// HUD do mundo: relógio/caixa/aspiração (topo), prompt contextual
/// ("[F] Interagir…") e as últimas linhas do log de eventos do GameWorld.
/// Só apresentação — quem produz o texto é o GameManager.
/// </summary>
public partial class GameHud : Control
{
    private const int MaxLogLines = 7;

    [Export] public Label StatusLabel { get; set; } = null!;
    [Export] public Label PromptLabel { get; set; } = null!;
    [Export] public Label LogLabel { get; set; } = null!;

    private readonly Queue<string> _log = new();

    public override void _Ready()
    {
        if (StatusLabel is null) throw new InvalidOperationException($"{nameof(GameHud)} requires {nameof(StatusLabel)}.");
        if (PromptLabel is null) throw new InvalidOperationException($"{nameof(GameHud)} requires {nameof(PromptLabel)}.");
        if (LogLabel is null) throw new InvalidOperationException($"{nameof(GameHud)} requires {nameof(LogLabel)}.");

        StatusLabel.Text = string.Empty;
        PromptLabel.Text = string.Empty;
        LogLabel.Text = string.Empty;
    }

    public void SetStatus(string text) => StatusLabel.Text = text;

    public void SetPrompt(string text) => PromptLabel.Text = text;

    public void Log(string message)
    {
        _log.Enqueue(message);
        while (_log.Count > MaxLogLines)
            _log.Dequeue();
        LogLabel.Text = string.Join("\n", _log);
    }
}
