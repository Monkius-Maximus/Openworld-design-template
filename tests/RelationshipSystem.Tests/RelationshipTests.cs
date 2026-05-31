using RelationshipSystem.Core;
using Xunit;

namespace RelationshipSystem.Tests;

public class RelationshipTests
{
    private static Relationship Build() =>
        new() { FromId = "alice", ToId = "bob" };

    private static RelationshipModifier Mod(string name, float value, float hours) =>
        new() { Name = name, Value = value, RemainingHours = hours };

    [Fact]
    public void Requires_distinct_endpoints_via_matrix()
    {
        var matrix = new RelationshipMatrix();

        Assert.Throws<ArgumentException>(() => matrix.Get("alice", "alice"));
    }

    [Fact]
    public void Add_modifier_changes_effective_score()
    {
        var rel = Build();
        rel.Value.ApplyDaily(20f);

        rel.AddModifier(Mod("Gift", 10f, 48f));

        Assert.Equal(30f, rel.EffectiveDaily);
        Assert.Equal(20f, rel.Value.Daily); // base intacto
    }

    [Fact]
    public void Effective_lifetime_includes_modifiers()
    {
        var rel = Build();
        rel.Value.ApplyLifetime(40f);
        rel.AddModifier(Mod("Gift", 10f, 48f));

        Assert.Equal(50f, rel.EffectiveLifetime);
    }

    [Fact]
    public void Modifiers_sum_together()
    {
        var rel = Build();
        rel.AddModifier(Mod("Gift", 10f, 48f));
        rel.AddModifier(Mod("Insult", -20f, 12f));

        Assert.Equal(-10f, rel.EffectiveDaily);
    }

    [Fact]
    public void Decay_modifiers_reduces_remaining_hours()
    {
        var rel = Build();
        rel.AddModifier(Mod("Gift", 10f, 48f));

        rel.DecayModifiers(24f);

        Assert.Single(rel.Modifiers);
        Assert.Equal(24f, rel.Modifiers[0].RemainingHours);
    }

    [Fact]
    public void Decay_modifiers_removes_expired()
    {
        var rel = Build();
        rel.AddModifier(Mod("Gift", 10f, 12f));

        rel.DecayModifiers(24f);

        Assert.Empty(rel.Modifiers);
    }

    [Fact]
    public void Expired_modifier_does_not_count_before_removal()
    {
        var rel = Build();
        var mod = Mod("Gift", 10f, 5f);
        rel.AddModifier(mod);

        mod.RemainingHours = 0f; // expira sem ainda ter sido varrido

        Assert.Equal(0f, rel.EffectiveDaily);
        Assert.False(rel.IsFurious);
    }

    [Fact]
    public void Is_furious_while_fury_modifier_active()
    {
        var rel = Build();
        rel.AddModifier(Mod(Relationship.FuryModifierName, -20f, RelationshipPhysics.FuryDurationHours));

        Assert.True(rel.IsFurious);

        rel.DecayModifiers(RelationshipPhysics.FuryDurationHours);

        Assert.False(rel.IsFurious);
    }

    [Fact]
    public void Add_null_modifier_throws()
    {
        var rel = Build();

        Assert.Throws<ArgumentNullException>(() => rel.AddModifier(null!));
    }

    [Fact]
    public void Blank_modifier_name_throws()
    {
        Assert.Throws<ArgumentException>(() => Mod("  ", 10f, 5f));
    }

    [Fact]
    public void Negative_hours_passed_throws()
    {
        var mod = Mod("Gift", 10f, 5f);

        Assert.Throws<ArgumentOutOfRangeException>(() => mod.DecayTime(-1f));
    }

    [Fact]
    public void Asymmetric_directions_are_independent()
    {
        var matrix = new RelationshipMatrix();

        matrix.Get("alice", "bob").Value.ApplyDaily(80f);   // amor unilateral
        matrix.Get("bob", "alice").Value.ApplyDaily(-30f);  // desinteresse

        Assert.Equal(80f, matrix.Get("alice", "bob").Value.Daily);
        Assert.Equal(-30f, matrix.Get("bob", "alice").Value.Daily);
        Assert.False(matrix.AreFriends("alice", "bob"));
    }
}
