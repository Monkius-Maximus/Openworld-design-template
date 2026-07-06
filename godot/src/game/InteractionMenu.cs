using System;
using System.Collections.Generic;
using EconomySystem.Core;
using EconomySystem.Core.Market;
using Godot;
using RelationshipSystem.Core;
using RelationshipSystem.Core.Interactions;
using WorldSimulation.Core;

namespace BuildSystem.Game;

/// <summary>
/// Menu de interação com um NPC: as interações sociais do InteractionLibrary
/// mais o presente pago (integração com a economia). A UI é montada em
/// código no _Ready; a cena só posiciona o painel. Botões indisponíveis
/// (pré-condição do núcleo não atendida, ou sem saldo) ficam desabilitados.
/// </summary>
public partial class InteractionMenu : PanelContainer
{
    private static readonly InteractionDefinition[] SocialOptions =
    {
        InteractionLibrary.Talk,
        InteractionLibrary.Compliment,
        InteractionLibrary.Flirt,
        InteractionLibrary.Insult,
    };

    private GameWorld? _world;
    private Npc3D? _target;

    private Label _title = null!;
    private Label _relationshipLabel = null!;
    private Label _resultLabel = null!;
    private VBoxContainer _buttonBox = null!;
    private readonly List<(Button Button, InteractionDefinition Def)> _socialButtons = new();
    private Button _giftButton = null!;

    public override void _Ready()
    {
        var layout = new VBoxContainer();
        AddChild(layout);

        _title = new Label();
        layout.AddChild(_title);

        _relationshipLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        layout.AddChild(_relationshipLabel);

        _buttonBox = new VBoxContainer();
        layout.AddChild(_buttonBox);

        foreach (var def in SocialOptions)
        {
            var captured = def;
            var button = new Button { Text = captured.DisplayName };
            button.Pressed += () => RunSocial(captured);
            _buttonBox.AddChild(button);
            _socialButtons.Add((button, captured));
        }

        _giftButton = new Button
        {
            Text = $"Dar Presente ({MarketRules.BaseCurrencySymbol}{SocialCosts.GiftCost})",
        };
        _giftButton.Pressed += RunGift;
        _buttonBox.AddChild(_giftButton);

        _resultLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(340f, 48f),
        };
        layout.AddChild(_resultLabel);

        var close = new Button { Text = "Fechar (Esc)" };
        close.Pressed += Close;
        layout.AddChild(close);

        Visible = false;
    }

    /// <summary>Chamado pelo GameManager depois de criar o mundo.</summary>
    public void Bind(GameWorld world) => _world = world ?? throw new ArgumentNullException(nameof(world));

    public void Open(Npc3D npc)
    {
        ArgumentNullException.ThrowIfNull(npc);
        _target = npc;
        _title.Text = $"Interagindo com {npc.DisplayName}";
        _resultLabel.Text = string.Empty;
        RefreshRelationship();
        RefreshAvailability();
        Visible = true;
    }

    public void Close()
    {
        Visible = false;
        _target = null;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Visible && @event.IsActionPressed("ui_cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    private void RunSocial(InteractionDefinition def)
    {
        if (_world is null || _target is null)
            return;

        InteractionOutcome outcome = _world.Perform(DemoWorldFactory.PlayerId, _target.CharacterId, def);
        AfterAction(outcome);
    }

    private void RunGift()
    {
        if (_world is null || _target is null)
            return;

        InteractionOutcome outcome = _world.GiveGift(DemoWorldFactory.PlayerId, _target.CharacterId);
        AfterAction(outcome);
    }

    private void AfterAction(InteractionOutcome outcome)
    {
        _resultLabel.Text = outcome.Message;
        RefreshRelationship();
        RefreshAvailability();
    }

    private void RefreshRelationship()
    {
        if (_world is null || _target is null)
            return;

        Relationship forward = _world.Relationships.Get(DemoWorldFactory.PlayerId, _target.CharacterId);
        Relationship back = _world.Relationships.Get(_target.CharacterId, DemoWorldFactory.PlayerId);

        string flags = forward.Flags.Count > 0 ? string.Join(", ", forward.Flags) : "—";
        _relationshipLabel.Text =
            $"Você → {_target.DisplayName}: amizade {forward.EffectiveDaily:0}/{forward.EffectiveLifetime:0}, " +
            $"romance {forward.EffectiveRomanceDaily:0}/{forward.EffectiveRomanceLifetime:0} [{flags}]\n" +
            $"{_target.DisplayName} → você: amizade {back.EffectiveDaily:0}/{back.EffectiveLifetime:0}, " +
            $"romance {back.EffectiveRomanceDaily:0}/{back.EffectiveRomanceLifetime:0}";
    }

    private void RefreshAvailability()
    {
        if (_world is null || _target is null)
            return;

        Relationship rel = _world.Relationships.Get(DemoWorldFactory.PlayerId, _target.CharacterId);
        foreach ((Button button, InteractionDefinition def) in _socialButtons)
            button.Disabled = !def.Available(rel);

        Household? home = _world.HouseholdOf(DemoWorldFactory.PlayerId);
        _giftButton.Disabled = !InteractionLibrary.GiveGift.Available(rel)
            || home is null
            || home.Funds.Balance < SocialCosts.GiftCost;
    }
}
