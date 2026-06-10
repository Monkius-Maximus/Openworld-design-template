using BattleSystem.Core;
using Xunit;

namespace BattleSystem.Tests;

public class BattleTests
{
    private static Combatant Make(string id, float speed = 5f, float health = 50f) =>
        new(new CombatantStats
        {
            Id = id,
            MaxHealth = health,
            MaxStamina = 50f,
            Attack = 10f,
            Defense = 5f,
            Speed = speed,
        });

    [Fact]
    public void Initiative_orders_by_speed_descending()
    {
        var lento = Make("lento", speed: 2f);
        var rapido = Make("rapido", speed: 9f);
        var medio = Make("medio", speed: 5f);

        var battle = new Battle("b1", new[] { lento, rapido }, new[] { medio });

        Assert.Equal(new[] { "rapido", "medio", "lento" }, battle.Initiative.Select(c => c.Id));
        Assert.Same(rapido, battle.Active);
    }

    [Fact]
    public void Duplicated_ids_are_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new Battle("b1", new[] { Make("x") }, new[] { Make("X") }));
    }

    [Fact]
    public void AdvanceTurn_skips_defeated_and_increments_round_on_wrap()
    {
        var a = Make("a", speed: 9f);
        var b = Make("b", speed: 5f);
        var c = Make("c", speed: 1f);
        var battle = new Battle("b1", new[] { a, b }, new[] { c });

        b.ApplyDamage(999f); // derrotado: deve ser pulado

        battle.AdvanceTurn();
        Assert.Same(c, battle.Active);
        Assert.Equal(1, battle.Round);

        battle.AdvanceTurn();
        Assert.Same(a, battle.Active);
        Assert.Equal(2, battle.Round); // deu a volta
    }

    [Fact]
    public void Winner_is_null_while_running_and_set_when_team_falls()
    {
        var a = Make("a");
        var b = Make("b");
        var battle = new Battle("b1", new[] { a }, new[] { b });

        Assert.False(battle.IsOver);
        Assert.Null(battle.Winner);

        b.ApplyDamage(999f);

        Assert.True(battle.IsOver);
        Assert.Equal(BattleTeam.A, battle.Winner);
    }

    [Fact]
    public void Allies_and_enemies_exclude_defeated()
    {
        var a1 = Make("a1");
        var a2 = Make("a2");
        var b1 = Make("b1");
        var b2 = Make("b2");
        var battle = new Battle("b1", new[] { a1, a2 }, new[] { b1, b2 });

        a2.ApplyDamage(999f);
        b2.ApplyDamage(999f);

        Assert.Empty(battle.AlliesOf(a1));
        Assert.Equal(new[] { "b1" }, battle.EnemiesOf(a1).Select(c => c.Id));
    }
}
