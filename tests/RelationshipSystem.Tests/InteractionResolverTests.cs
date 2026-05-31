using RelationshipSystem.Core;
using RelationshipSystem.Core.Interactions;
using Xunit;

namespace RelationshipSystem.Tests;

public class InteractionResolverTests
{
    [Fact]
    public void Unavailable_interaction_fails_fast()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        // Talk exige daily > -50; forçamos abaixo disso.
        matrix.Get("alice", "bob").Value.ApplyDaily(-60f);

        Assert.Throws<InvalidOperationException>(
            () => resolver.Perform("alice", "bob", InteractionLibrary.Talk));
    }

    [Fact]
    public void Accepted_interaction_applies_accept_effects()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        bool accepted = resolver.Perform("alice", "bob", InteractionLibrary.Talk);

        Assert.True(accepted);
        Assert.Equal(3f, matrix.Get("alice", "bob").Value.Daily);
        Assert.Equal(1f, matrix.Get("alice", "bob").Value.Lifetime);
    }

    [Fact]
    public void Gift_attaches_a_modifier()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        resolver.Perform("alice", "bob", InteractionLibrary.GiveGift);

        var rel = matrix.Get("alice", "bob");
        Assert.Single(rel.Modifiers);
        Assert.Equal(48f, rel.Modifiers[0].RemainingHours);
        // 10 daily + 10 modificador
        Assert.Equal(20f, rel.EffectiveDaily);
    }

    [Fact]
    public void Mutual_high_daily_forms_friendship_and_raises_event()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        // Pré-carrega o lado de Bob acima do limiar de amizade.
        matrix.Get("bob", "alice").Value.ApplyDaily(60f);
        // E o lado de Alice quase lá.
        matrix.Get("alice", "bob").Value.ApplyDaily(48f);

        Relationship? formed = null;
        resolver.FriendshipFormed += rel => formed = rel;

        // Talk dá +3 a Alice→Bob, cruzando 50 com ambos os lados mútuos.
        resolver.Perform("alice", "bob", InteractionLibrary.Talk);

        Assert.True(matrix.AreFriends("alice", "bob"));
        Assert.NotNull(formed);
        Assert.Contains(RelationshipFlag.Friend, matrix.Get("alice", "bob").Flags);
    }

    [Fact]
    public void Gift_modifier_is_cloned_per_relationship()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        resolver.Perform("alice", "bob", InteractionLibrary.GiveGift);
        resolver.Perform("carol", "dave", InteractionLibrary.GiveGift);

        var first = matrix.Get("alice", "bob").Modifiers[0];
        var second = matrix.Get("carol", "dave").Modifiers[0];

        first.RemainingHours = 1f;

        Assert.NotSame(first, second);
        Assert.Equal(48f, second.RemainingHours);
    }

    [Fact]
    public void Null_matrix_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new InteractionResolver(null!));
    }
}
