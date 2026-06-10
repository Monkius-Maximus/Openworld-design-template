using BattleSystem.Core;
using Xunit;

namespace BattleSystem.Tests;

public class TurnSystemTests
{
    private static Combatant Make(string id, float speed, float health = 100f, float stamina = 50f) =>
        new(new CombatantStats
        {
            Id = id,
            MaxHealth = health,
            MaxStamina = stamina,
            Attack = 10f,
            Defense = 5f,
            Speed = speed,
        });

    [Fact]
    public void EndTurn_applies_poison_ticks_statuses_regens_and_advances()
    {
        var a = Make("a", speed: 9f);
        var b = Make("b", speed: 1f);
        var battle = new Battle("b1", new[] { a }, new[] { b });
        var turns = new TurnSystem(new BattleResolver(battle));

        a.TrySpendStamina(20f); // 30 restantes
        a.AddStatus(new StatusEffect { Name = "Envenenado", DamagePerTurn = 3f, RemainingTurns = 2 });

        turns.EndTurn();

        Assert.Equal(97f, a.Health);                                  // veneno
        Assert.Equal(1, a.Statuses.Single().RemainingTurns);          // envelheceu
        Assert.Equal(30f + CombatPhysics.StaminaRegenPerTurn, a.Stamina);
        Assert.Same(b, battle.Active);                                // avançou
    }

    [Fact]
    public void Poison_can_end_the_battle_and_announces_once()
    {
        var a = Make("a", speed: 9f, health: 2f);
        var b = Make("b", speed: 1f);
        var battle = new Battle("b1", new[] { a }, new[] { b });
        var resolver = new BattleResolver(battle);
        var turns = new TurnSystem(resolver);

        a.AddStatus(new StatusEffect { Name = "Envenenado", DamagePerTurn = 5f, RemainingTurns = 3 });

        string? defeatedId = null;
        int endCount = 0;
        turns.CombatantDefeated += c => defeatedId = c.Id;
        resolver.BattleEnded += (_, _) => endCount++;

        turns.EndTurn();

        Assert.Equal("a", defeatedId);
        Assert.True(battle.IsOver);
        Assert.Equal(BattleTeam.B, battle.Winner);
        Assert.Equal(1, endCount);

        Assert.Throws<InvalidOperationException>(turns.EndTurn);
    }
}
