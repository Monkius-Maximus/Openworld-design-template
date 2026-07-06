using System;
using System.Collections.Generic;
using BuildSystem.Movement;
using EconomySystem.Core.Market;
using Godot;
using WorldSimulation.Core;

namespace BuildSystem.Game;

/// <summary>
/// Maestro da cena 3D: dono do <see cref="GameWorld"/> (montado pelo
/// DemoWorldFactory sobre o mercado carregado pelo MarketController), avança
/// o relógio do mundo a partir do delta real, decide quem detém o input
/// (jogador × menus × modo construção — único dono de
/// <c>Player.ControlEnabled</c>) e alimenta o HUD com status, prompt e log.
/// Deve ser o ÚLTIMO nó da cena, para que os _Ready dos demais já tenham rodado.
/// </summary>
public partial class GameManager : Node
{
    [Export] public PlayerController3D Player { get; set; } = null!;
    [Export] public MarketController MarketController { get; set; } = null!;
    [Export] public GameHud Hud { get; set; } = null!;
    [Export] public InteractionMenu Menu { get; set; } = null!;
    [Export] public BuildController3D Build { get; set; } = null!;

    /// <summary>Ritmo do tempo: minutos de jogo por segundo real (2 → 1 dia = 12 min).</summary>
    [Export] public float GameMinutesPerRealSecond { get; set; } = 2f;

    public GameWorld World { get; private set; } = null!;

    private readonly List<Npc3D> _npcs = new();
    private Npc3D? _nearest;
    private double _statusTimer;

    public override void _Ready()
    {
        if (Player is null) throw new InvalidOperationException($"{nameof(GameManager)} requires {nameof(Player)}.");
        if (MarketController is null) throw new InvalidOperationException($"{nameof(GameManager)} requires {nameof(MarketController)}.");
        if (Hud is null) throw new InvalidOperationException($"{nameof(GameManager)} requires {nameof(Hud)}.");
        if (Menu is null) throw new InvalidOperationException($"{nameof(GameManager)} requires {nameof(Menu)}.");
        if (Build is null) throw new InvalidOperationException($"{nameof(GameManager)} requires {nameof(Build)}.");

        World = DemoWorldFactory.Create(MarketController.Market);
        World.EventLogged += message => Hud.Log(message);
        Menu.Bind(World);

        foreach (Node node in GetTree().GetNodesInGroup(Npc3D.GroupName))
            if (node is Npc3D npc)
                _npcs.Add(npc);

        Hud.Log("Mundo iniciado: aproxime-se de um vizinho e pressione F.");
        Hud.Log("B constrói, M abre o mercado, botão direito mira.");
    }

    public override void _Process(double delta)
    {
        World.AdvanceMinutes((float)delta * GameMinutesPerRealSecond);

        _nearest = FindNearestNpcInRange();

        // Um único dono do input: menus abertos pausam jogador E construção;
        // o modo construção pausa só o jogador (a câmera continua livre).
        bool uiOpen = Menu.Visible || MarketController.Panel.Visible;
        Build.InputEnabled = !uiOpen;
        Player.ControlEnabled = !uiOpen && !Build.BuildModeActive;

        Hud.SetPrompt(!uiOpen && !Build.BuildModeActive && _nearest is not null
            ? $"[F] Interagir com {_nearest.DisplayName}"
            : string.Empty);

        _statusTimer += delta;
        if (_statusTimer >= 0.25)
        {
            _statusTimer = 0;
            Hud.SetStatus(BuildStatusText());
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact")
            && _nearest is not null
            && !Menu.Visible
            && !MarketController.Panel.Visible
            && !Build.BuildModeActive)
        {
            Menu.Open(_nearest);
            GetViewport().SetInputAsHandled();
        }
    }

    private Npc3D? FindNearestNpcInRange()
    {
        Npc3D? nearest = null;
        float bestDistance = float.MaxValue;
        foreach (Npc3D npc in _npcs)
        {
            if (!npc.PlayerInRange)
                continue;
            float distance = npc.GlobalPosition.DistanceSquaredTo(Player.GlobalPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = npc;
            }
        }
        return nearest;
    }

    private string BuildStatusText()
    {
        SimulationCalendar calendar = World.Market.Calendar;
        var home = World.HouseholdOf(DemoWorldFactory.PlayerId);
        var meter = World.WantsAndFears.MeterFor(DemoWorldFactory.PlayerId);

        return $"Dia {calendar.CurrentDay} ({GameWorld.DayName(calendar.DayOfWeek)}) " +
               $"{World.Clock.HourOfDay:00}:{World.Clock.MinuteOfHour:00}   |   " +
               $"{MarketRules.BaseCurrencySymbol}{home?.Funds.Balance ?? 0}   |   " +
               $"Aspiração {meter.Score:0}";
    }
}
