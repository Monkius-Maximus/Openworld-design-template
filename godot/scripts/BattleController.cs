using BattleSystem.Core;
using BattleSystem.Core.Actions;
using BattleSystem.Core.Ai;
using BattleSystem.Core.Integration;
using Godot;
using RelationshipSystem.Core;

namespace BattleSystem.Godot;

/// <summary>
/// Adaptador Godot da etapa de batalhas — a camada FINA entre a engine e o
/// núcleo agnóstico (<c>BattleSystem.Core</c>). Tradução dos padrões Unreal:
/// o que lá seria um Actor/GameMode com dispatchers, aqui é um Node que
/// assina os eventos C# do resolver e os re-emite como <c>[Signal]</c>.
///
/// Funciona SEM NENHUM ASSET: toda a UI (barras de vida, log, botão) é
/// construída em código com nós nativos (ProgressBar, RichTextLabel, Button).
/// Quando os assets open source chegarem (ver docs/batalha-design.md),
/// substitua esta UI por cenas próprias — o núcleo não muda.
/// </summary>
public partial class BattleController : Control
{
    [Signal]
    public delegate void TurnPlayedEventHandler(string actorId, string actionId, string targetId, bool hit);

    [Signal]
    public delegate void CombatantDefeatedEventHandler(string combatantId);

    [Signal]
    public delegate void BattleFinishedEventHandler(string winnerTeam);

    private Battle _battle = null!;
    private BattleResolver _resolver = null!;
    private TurnSystem _turns = null!;
    private RelationshipBattleBridge _bridge = null!;
    private readonly SimpleBattleAI _ai = new();
    private List<BattleActionDefinition> _actions = null!;

    private RichTextLabel _log = null!;
    private Button _nextTurnButton = null!;
    private readonly Dictionary<string, ProgressBar> _healthBars = new();

    public override void _Ready()
    {
        SetupBattle();
        BuildUi();
        RefreshBars();
        Append("[b]Batalha iniciada.[/b] Aria & Bruno (amigos) vs Caio & Duda — Aria odeia Caio.");
    }

    /// <summary>
    /// Monta o MESMO cenário do demo de console (samples/BattleSystem.Demo):
    /// matriz de relacionamentos → moral pré-batalha → IA nos dois lados.
    /// </summary>
    private void SetupBattle()
    {
        var matrix = new RelationshipMatrix();
        matrix.Get("Aria", "Bruno").Value.ApplyDaily(65f);
        matrix.Get("Bruno", "Aria").Value.ApplyDaily(65f);
        matrix.Get("Aria", "Caio").Value.ApplyDaily(-70f);

        static Combatant Make(string id, float hp, float sp, float atk, float def, float spd) =>
            new(new CombatantStats
            {
                Id = id, MaxHealth = hp, MaxStamina = sp,
                Attack = atk, Defense = def, Speed = spd,
            });

        _battle = new Battle("praca-central",
            new[] { Make("Aria", 90f, 60f, 12f, 5f, 9f), Make("Bruno", 110f, 50f, 9f, 7f, 5f) },
            new[] { Make("Caio", 100f, 55f, 11f, 6f, 7f), Make("Duda", 85f, 65f, 10f, 4f, 6f) });

        _resolver = new BattleResolver(_battle);
        _turns = new TurnSystem(_resolver);
        _bridge = new RelationshipBattleBridge(matrix);
        _actions = BattleActionLibrary.All.Values.ToList();

        _bridge.ApplyPreBattleMorale(_battle);

        // Eventos C# do núcleo → sinais Godot + log na tela.
        _resolver.ActionPerformed += (actor, target, def, hit) =>
        {
            Append($"{actor.Id} usa [i]{def.DisplayName}[/i] em {(actor == target ? "si mesmo" : target.Id)}: " +
                   (hit ? "acertou" : "[color=orange]errou[/color]"));
            EmitSignal(SignalName.TurnPlayed, actor.Id, def.Id, target.Id, hit);
        };
        _resolver.DamageDealt += (_, target, amount) =>
            Append($"    → {target.Id} sofre [color=red]{amount:0.#}[/color] de dano");
        _resolver.CombatantDefeated += OnDefeated;
        _turns.CombatantDefeated += OnDefeated;
        _resolver.BattleEnded += (_, winner) =>
        {
            _bridge.ApplyPostBattleOutcome(_battle);
            Append($"[b]Fim da batalha: vence o time {winner}![/b] Desfecho gravado na matriz de relacionamentos.");
            _nextTurnButton.Disabled = true;
            EmitSignal(SignalName.BattleFinished, winner.ToString());
        };
    }

    /// <summary>UI 100% por código, só com nós nativos — zero assets.</summary>
    private void BuildUi()
    {
        var root = new VBoxContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);

        var bars = new HBoxContainer();
        bars.AddThemeConstantOverride("separation", 16);
        root.AddChild(bars);

        foreach (var c in _battle.Initiative)
        {
            var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddChild(new Label { Text = $"{c.Id} (time {_battle.TeamOf(c)})" });
            var bar = new ProgressBar { MaxValue = c.Stats.MaxHealth, Value = c.Health, ShowPercentage = false };
            _healthBars[c.Id] = bar;
            box.AddChild(bar);
            bars.AddChild(box);
        }

        _log = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(_log);

        _nextTurnButton = new Button { Text = "Próximo turno" };
        _nextTurnButton.Pressed += PlayOneTurn;
        root.AddChild(_nextTurnButton);
    }

    /// <summary>Um turno por clique: a IA decide pelo combatente da vez.</summary>
    private void PlayOneTurn()
    {
        if (_battle.IsOver)
            return;

        var actor = _battle.Active;
        var (action, target) = _ai.Choose(_battle, actor, _actions);
        _resolver.Perform(actor.Id, action, target.Id);
        if (!_battle.IsOver)
            _turns.EndTurn();

        RefreshBars();
    }

    private void OnDefeated(Combatant c)
    {
        Append($"    ✖ [b]{c.Id} foi derrotado![/b]");
        EmitSignal(SignalName.CombatantDefeated, c.Id);
    }

    private void RefreshBars()
    {
        foreach (var c in _battle.Initiative)
            _healthBars[c.Id].Value = c.Health;
    }

    private void Append(string bbcode) => _log.AppendText(bbcode + "\n");
}
