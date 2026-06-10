using BattleSystem.Core;
using BattleSystem.Core.Integration;
using RelationshipSystem.Core;
using Xunit;

namespace BattleSystem.Tests;

public class RelationshipBattleBridgeTests
{
    private static Combatant Make(string id, float speed = 5f) =>
        new(new CombatantStats
        {
            Id = id,
            MaxHealth = 100f,
            MaxStamina = 50f,
            Attack = 10f,
            Defense = 5f,
            Speed = speed,
        });

    [Fact]
    public void Friends_on_the_same_team_gain_morale()
    {
        var matrix = new RelationshipMatrix();
        matrix.Get("a1", "a2").Value.ApplyDaily(60f);
        matrix.Get("a2", "a1").Value.ApplyDaily(60f); // amizade é mútua

        var a1 = Make("a1");
        var a2 = Make("a2");
        var b1 = Make("b1");
        var battle = new Battle("b1", new[] { a1, a2 }, new[] { b1 });

        new RelationshipBattleBridge(matrix).ApplyPreBattleMorale(battle);

        Assert.Contains(a1.Statuses, s => s.Name == MoraleRules.FriendMoraleName);
        Assert.Contains(a2.Statuses, s => s.Name == MoraleRules.FriendMoraleName);
        Assert.Empty(b1.Statuses);

        Assert.Equal(10f + MoraleRules.FriendAttackBonus, a1.EffectiveAttack);
    }

    [Fact]
    public void Facing_a_rival_triggers_fury()
    {
        var matrix = new RelationshipMatrix();
        matrix.Get("a1", "b1").Value.ApplyDaily(-80f); // rival declarado

        var a1 = Make("a1");
        var b1 = Make("b1");
        var battle = new Battle("b1", new[] { a1 }, new[] { b1 });

        new RelationshipBattleBridge(matrix).ApplyPreBattleMorale(battle);

        var fury = Assert.Single(a1.Statuses);
        Assert.Equal(MoraleRules.RivalFuryName, fury.Name);
        Assert.Equal(10f + MoraleRules.RivalAttackBonus, a1.EffectiveAttack);
        Assert.Equal(5f + MoraleRules.RivalDefensePenalty, a1.EffectiveDefense);

        Assert.Empty(b1.Statuses); // a rivalidade é direcional (b1 não odeia a1)
    }

    [Fact]
    public void Outcome_writes_modifiers_back_to_the_matrix()
    {
        var matrix = new RelationshipMatrix();
        var a1 = Make("a1");
        var a2 = Make("a2");
        var b1 = Make("b1");
        var battle = new Battle("b1", new[] { a1, a2 }, new[] { b1 });
        var bridge = new RelationshipBattleBridge(matrix);

        Assert.Throws<InvalidOperationException>(() => bridge.ApplyPostBattleOutcome(battle));

        b1.ApplyDamage(999f); // time A vence

        bridge.ApplyPostBattleOutcome(battle);

        // perdedor → cada vencedor: mágoa temporária
        Assert.Contains(matrix.Get("b1", "a1").Modifiers, m => m.Name == MoraleRules.DefeatModifierName);
        Assert.Contains(matrix.Get("b1", "a2").Modifiers, m => m.Name == MoraleRules.DefeatModifierName);
        Assert.Equal(MoraleRules.DefeatModifierValue, matrix.Get("b1", "a1").EffectiveDaily);

        // vencedores aliados: modificador mútuo
        Assert.Contains(matrix.Get("a1", "a2").Modifiers, m => m.Name == MoraleRules.ComradeModifierName);
        Assert.Contains(matrix.Get("a2", "a1").Modifiers, m => m.Name == MoraleRules.ComradeModifierName);
    }
}
