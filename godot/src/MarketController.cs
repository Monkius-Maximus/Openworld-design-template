using System;
using System.IO;
using EconomySystem.Core.Market;
using Godot;

namespace BuildSystem;

/// <summary>
/// Dono do <see cref="CurrencyMarket"/> no jogo: carrega/salva o estado em
/// <c>user://market_state.json</c> e alterna o painel de moedas pela ação
/// <c>market_toggle</c> (tecla M). Espelha o estilo do BuildController:
/// dependências [Export] validadas em _Ready.
/// </summary>
public partial class MarketController : Node
{
    [Export] public CurrencyManagerPanel Panel { get; set; } = null!;

    private string _savePath = string.Empty;

    public CurrencyMarket Market { get; private set; } = null!;

    public override void _Ready()
    {
        if (Panel is null) throw new InvalidOperationException($"{nameof(MarketController)} requires {nameof(Panel)}.");

        _savePath = ProjectSettings.GlobalizePath("user://market_state.json");
        Market = Load();
        Panel.Visible = false;
        Panel.Refresh();
    }

    /// <summary>Persiste o mercado no arquivo de save do usuário.</summary>
    public void Save()
    {
        var dir = Path.GetDirectoryName(_savePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_savePath, MarketStateSerializer.ToJson(Market));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("market_toggle"))
        {
            Panel.Visible = !Panel.Visible;
            if (Panel.Visible)
                Panel.Refresh();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && Market is not null)
            Save();
    }

    private CurrencyMarket Load()
    {
        try
        {
            if (File.Exists(_savePath))
                return MarketStateSerializer.FromJson(File.ReadAllText(_savePath));
        }
        catch (Exception e)
        {
            GD.PushWarning($"Save de mercado inválido em '{_savePath}' ({e.Message}); começando um novo.");
        }
        return new CurrencyMarket();
    }
}
