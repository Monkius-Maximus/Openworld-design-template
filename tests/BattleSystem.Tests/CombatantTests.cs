using BattleSystem.Core;
using Xunit;

namespace BattleSystem.Tests;

public class CombatantTests
{
    private static CombatantStats Stats(string id = "heroi", float health = 100f, float stamina = 50f,
        float attack = 10f, float defense = 5f, float speed = 7f) => new()
    {
        Id = id,
        MaxHealth = health,
        MaxStamina = stamina,
        Attack = attack,
        Defense = defense,
        Speed = speed,
    };

    [Fact]
    public void Stats_validation_is_fail_fast()
    {
        Assert.Throws<ArgumentException>(() => Stats(id: " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => Stats(health: 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Stats(stamina: -1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Stats(attack: -1f));
    }

    [Fact]
    public void Starts_with_full_health_and_stamina()
    {
        var c = new Combatant(Stats());
        Assert.Equal(100f, c.Health);
        Assert.Equal(50f, c.Stamina);
        Assert.False(c.IsDefeated);
    }

    [Fact]
    public void Damage_never_drops_health_below_zero_and_reports_actual()
    {
        var c = new Combatant(Stats(health: 30f));
        Assert.Equal(30f, c.ApplyDamage(45f));
        Assert.Equal(0f, c.Health);
        Assert.True(c.IsDefeated);
    }

    [Fact]
    public void Heal_caps_at_max_and_does_not_revive()
    {
        var c = new Combatant(Stats(health: 100f));
        c.ApplyDamage(10f);
        c.Heal(50f);
        Assert.Equal(100f, c.Health);

        c.ApplyDamage(100f);
        c.Heal(50f);
        Assert.True(c.IsDefeated); // derrotado permanece derrotado
    }

    [Fact]
    public void Stamina_spend_fails_when_insufficient()
    {
        var c = new Combatant(Stats(stamina: 20f));
        Assert.True(c.TrySpendStamina(15f));
        Assert.False(c.TrySpendStamina(10f));
        Assert.Equal(5f, c.Stamina);

        c.RegenStamina(100f);
        Assert.Equal(20f, c.Stamina); // clamp no máximo
    }

    [Fact]
    public void Effective_stats_ignore_expired_statuses()
    {
        var c = new Combatant(Stats(attack: 10f, defense: 5f));
        c.AddStatus(new StatusEffect { Name = "Moral", AttackBonus = 5f, RemainingTurns = 2 });
        c.AddStatus(new StatusEffect { Name = "Em guarda", DefenseBonus = 8f, RemainingTurns = 0 });

        Assert.Equal(15f, c.EffectiveAttack);
        Assert.Equal(5f, c.EffectiveDefense); // status expirado não conta
    }

    [Fact]
    public void TickStatuses_removes_expired()
    {
        var c = new Combatant(Stats());
        c.AddStatus(new StatusEffect { Name = "Curto", AttackBonus = 1f, RemainingTurns = 1 });
        c.AddStatus(new StatusEffect { Name = "Longo", AttackBonus = 1f, RemainingTurns = 3 });

        c.TickStatuses();

        Assert.Single(c.Statuses);
        Assert.Equal("Longo", c.Statuses[0].Name);
    }

    [Fact]
    public void Status_clone_does_not_share_remaining_turns()
    {
        var template = new StatusEffect { Name = "Envenenado", DamagePerTurn = 3f, RemainingTurns = 3 };
        var clone = template.Clone();

        clone.TickTurn();

        Assert.Equal(3, template.RemainingTurns);
        Assert.Equal(2, clone.RemainingTurns);
    }
}
