using BattleSystem.Core;
using BattleSystem.Core.Actions;
using BattleSystem.Core.Ai;
using Xunit;

namespace BattleSystem.Tests;

public class SimpleBattleAITests
{
    private static Combatant Make(string id, float speed = 5f, float health = 100f, float stamina = 50f) =>
        new(new CombatantStats
        {
            Id = id,
            MaxHealth = health,
            MaxStamina = stamina,
            Attack = 10f,
            Defense = 5f,
            Speed = speed,
        });

    private static readonly IReadOnlyList<BattleActionDefinition> Pool =
        BattleActionLibrary.All.Values.ToList();

    [Fact]
    public void Heals_itself_when_in_danger()
    {
        var a = Make("a", speed: 9f);
        var b = Make("b", speed: 1f);
        var battle = new Battle("b1", new[] { a }, new[] { b });

        a.ApplyDamage(80f); // 20% de vida

        var (action, target) = new SimpleBattleAI().Choose(battle, a, Pool);

        Assert.Equal("FirstAid", action.Id);
        Assert.Same(a, target);
    }

    [Fact]
    public void Picks_strongest_affordable_attack_on_weakest_enemy()
    {
        var a = Make("a", speed: 9f, stamina: 50f);
        var b1 = Make("b1", speed: 2f);
        var b2 = Make("b2", speed: 1f);
        var battle = new Battle("b1", new[] { a }, new[] { b1, b2 });

        b2.ApplyDamage(40f); // mais ferido

        var (action, target) = new SimpleBattleAI().Choose(battle, a, Pool);

        Assert.Equal("HeavyBlow", action.Id); // maior dano que a stamina paga
        Assert.Same(b2, target);
    }

    [Fact]
    public void Rests_when_out_of_stamina()
    {
        var a = Make("a", speed: 9f, stamina: 50f);
        var b = Make("b", speed: 1f);
        var battle = new Battle("b1", new[] { a }, new[] { b });

        a.TrySpendStamina(48f); // 2 restantes: nenhum ataque pagável

        var (action, target) = new SimpleBattleAI().Choose(battle, a, Pool);

        Assert.Equal("Rest", action.Id);
        Assert.Same(a, target);
    }

    [Fact]
    public void Ai_driven_battle_reaches_an_end()
    {
        var a1 = Make("a1", speed: 9f);
        var a2 = Make("a2", speed: 7f);
        var b1 = Make("b1", speed: 5f);
        var b2 = Make("b2", speed: 3f);
        var battle = new Battle("b1", new[] { a1, a2 }, new[] { b1, b2 });
        var resolver = new BattleResolver(battle, new Random(7)); // determinístico
        var turns = new TurnSystem(resolver);
        var ai = new SimpleBattleAI();

        int guard = 0;
        while (!battle.IsOver && guard++ < 500)
        {
            var actor = battle.Active;
            var (action, target) = ai.Choose(battle, actor, Pool);
            resolver.Perform(actor.Id, action, target.Id);
            if (!battle.IsOver)
                turns.EndTurn();
        }

        Assert.True(battle.IsOver, "a batalha guiada por IA deve terminar");
        Assert.NotNull(battle.Winner);
    }
}
