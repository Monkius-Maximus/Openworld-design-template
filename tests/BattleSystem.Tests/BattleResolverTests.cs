using BattleSystem.Core;
using BattleSystem.Core.Actions;
using Xunit;

namespace BattleSystem.Tests;

public class BattleResolverTests
{
    private static Combatant Make(string id, float speed = 5f, float health = 100f,
        float attack = 10f, float defense = 5f, float stamina = 50f) =>
        new(new CombatantStats
        {
            Id = id,
            MaxHealth = health,
            MaxStamina = stamina,
            Attack = attack,
            Defense = defense,
            Speed = speed,
        });

    private static BattleActionDefinition SureHit(float damage) => new()
    {
        Id = "SureHit",
        DisplayName = "Golpe Certeiro",
        StaminaCost = 0f,
        Target = ActionTarget.Enemy,
        Available = (_, target) => !target.IsDefeated,
        HitChance = (_, _) => 1.0,
        OnHit = new BattleActionEffect(Damage: damage),
        OnMiss = new BattleActionEffect(),
    };

    [Fact]
    public void Damage_formula_is_base_plus_attack_minus_defense()
    {
        var atk = Make("atk", speed: 9f, attack: 10f);
        var def = Make("def", speed: 1f, defense: 4f);
        var battle = new Battle("b1", new[] { atk }, new[] { def });
        var resolver = new BattleResolver(battle);

        float dealt = 0f;
        resolver.DamageDealt += (_, _, amount) => dealt = amount;

        resolver.Perform("atk", SureHit(8f), "def");

        Assert.Equal(14f, dealt); // 8 + 10 - 4
        Assert.Equal(86f, def.Health);
    }

    [Fact]
    public void Damage_never_falls_below_minimum()
    {
        var atk = Make("atk", speed: 9f, attack: 0f);
        var def = Make("def", speed: 1f, defense: 50f);
        var battle = new Battle("b1", new[] { atk }, new[] { def });
        var resolver = new BattleResolver(battle);

        resolver.Perform("atk", SureHit(2f), "def");

        Assert.Equal(100f - CombatPhysics.MinDamage, def.Health);
    }

    [Fact]
    public void Miss_applies_OnMiss_effects()
    {
        var atk = Make("atk", speed: 9f);
        var def = Make("def", speed: 1f);
        var battle = new Battle("b1", new[] { atk }, new[] { def });
        var resolver = new BattleResolver(battle);

        var neverHits = new BattleActionDefinition
        {
            Id = "Wild",
            DisplayName = "Golpe Selvagem",
            StaminaCost = 0f,
            Target = ActionTarget.Enemy,
            Available = (_, t) => !t.IsDefeated,
            HitChance = (_, _) => 0.0,
            OnHit = new BattleActionEffect(Damage: 99f),
            OnMiss = new BattleActionEffect(SelfStatus: new StatusEffect
            {
                Name = "Desequilibrado",
                DefenseBonus = -5f,
                RemainingTurns = 1,
            }),
        };

        bool hit = resolver.Perform("atk", neverHits, "def");

        Assert.False(hit);
        Assert.Equal(100f, def.Health);
        Assert.Contains(atk.Statuses, s => s.Name == "Desequilibrado");
    }

    [Fact]
    public void Status_templates_are_cloned_per_target()
    {
        var atk = Make("atk", speed: 9f, stamina: 100f);
        var d1 = Make("d1", speed: 2f);
        var d2 = Make("d2", speed: 1f);
        var battle = new Battle("b1", new[] { atk }, new[] { d1, d2 });
        var resolver = new BattleResolver(battle, new Random(42));
        var turns = new TurnSystem(resolver);

        // Acerta os dois com a lâmina envenenada (forçando acerto).
        var poison = new BattleActionDefinition
        {
            Id = "P",
            DisplayName = "P",
            StaminaCost = 0f,
            Target = ActionTarget.Enemy,
            Available = (_, t) => !t.IsDefeated,
            HitChance = (_, _) => 1.0,
            OnHit = BattleActionLibrary.PoisonedBlade.OnHit,
            OnMiss = new BattleActionEffect(),
        };

        resolver.Perform("atk", poison, "d1");
        turns.EndTurn(); // d1 sofre veneno e o status dele envelhece
        resolver.Perform("d1", BattleActionLibrary.Rest);
        turns.EndTurn();
        resolver.Perform("d2", BattleActionLibrary.Rest);
        turns.EndTurn();
        resolver.Perform("atk", poison, "d2");

        int turnsD1 = d1.Statuses.Single(s => s.Name == "Envenenado").RemainingTurns;
        int turnsD2 = d2.Statuses.Single(s => s.Name == "Envenenado").RemainingTurns;

        Assert.Equal(3, turnsD2);      // recém-aplicado
        Assert.True(turnsD1 < 3);      // o do d1 já envelheceu de forma independente
    }

    [Fact]
    public void Fails_fast_out_of_turn_wrong_target_or_no_stamina()
    {
        var a = Make("a", speed: 9f, stamina: 5f);
        var ally = Make("ally", speed: 8f);
        var b = Make("b", speed: 1f);
        var battle = new Battle("b1", new[] { a, ally }, new[] { b });
        var resolver = new BattleResolver(battle);

        // não é o turno de "b"
        Assert.Throws<InvalidOperationException>(() =>
            resolver.Perform("b", SureHit(1f), "a"));

        // alvo aliado para ação de inimigo
        Assert.Throws<InvalidOperationException>(() =>
            resolver.Perform("a", SureHit(1f), "ally"));

        // stamina insuficiente (QuickStrike custa 10, "a" tem 5)
        Assert.Throws<InvalidOperationException>(() =>
            resolver.Perform("a", BattleActionLibrary.QuickStrike, "b"));
    }

    [Fact]
    public void Defeat_and_battle_end_events_fire_once()
    {
        var a = Make("a", speed: 9f, attack: 100f);
        var b = Make("b", speed: 1f, health: 10f);
        var battle = new Battle("b1", new[] { a }, new[] { b });
        var resolver = new BattleResolver(battle);

        var defeated = new List<string>();
        BattleTeam? winner = null;
        int endCount = 0;
        resolver.CombatantDefeated += c => defeated.Add(c.Id);
        resolver.BattleEnded += (_, w) => { winner = w; endCount++; };

        resolver.Perform("a", SureHit(50f), "b");

        Assert.Equal(new[] { "b" }, defeated);
        Assert.Equal(BattleTeam.A, winner);
        Assert.Equal(1, endCount);

        // batalha encerrada: qualquer nova ação falha rápido
        Assert.Throws<InvalidOperationException>(() =>
            resolver.Perform("a", SureHit(1f), "b"));
    }

    [Fact]
    public void Heal_and_rest_actions_work()
    {
        var a = Make("a", speed: 9f, stamina: 50f);
        var b = Make("b", speed: 1f);
        var battle = new Battle("b1", new[] { a }, new[] { b });
        var resolver = new BattleResolver(battle);

        a.ApplyDamage(30f);
        a.TrySpendStamina(40f); // fica com 10

        resolver.Perform("a", BattleActionLibrary.Rest);
        Assert.Equal(40f, a.Stamina); // 10 + 30

        var turns = new TurnSystem(resolver);
        turns.EndTurn();
        resolver.Perform("b", BattleActionLibrary.Rest);
        turns.EndTurn();

        resolver.Perform("a", BattleActionLibrary.FirstAid, "a");
        Assert.Equal(90f, a.Health); // 70 + 20
    }
}
