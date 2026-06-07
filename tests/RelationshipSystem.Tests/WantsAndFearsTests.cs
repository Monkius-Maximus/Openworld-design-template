using RelationshipSystem.Core;
using RelationshipSystem.Core.Interactions;
using Xunit;

namespace RelationshipSystem.Tests;

public class WantsAndFearsTests
{
    [Fact]
    public void Aspiration_meter_clamps_to_bounds()
    {
        var m = new AspirationMeter();

        m.Apply(150);
        Assert.Equal(AspirationMeter.Max, m.Score);

        m.Apply(-300);
        Assert.Equal(AspirationMeter.Min, m.Score);
    }

    [Fact]
    public void Fulfilling_a_want_raises_aspiration_fires_event_and_removes_it()
    {
        var matrix = new RelationshipMatrix();
        var wf = new WantsAndFearsSystem();
        wf.Add("alice", RelationshipDesires.BefriendWant("bob", value: 25));

        Desire? fulfilled = null;
        wf.WantFulfilled += (_, d) => fulfilled = d;

        matrix.Get("alice", "bob").Flags.Add(RelationshipFlag.Friend);
        wf.Evaluate(matrix, "alice");

        Assert.Equal(25f, wf.MeterFor("alice").Score);
        Assert.NotNull(fulfilled);
        Assert.Empty(wf.DesiresOf("alice")); // one-shot
    }

    [Fact]
    public void Realizing_a_fear_lowers_aspiration_and_fires_event()
    {
        var matrix = new RelationshipMatrix();
        var wf = new WantsAndFearsSystem();
        wf.Add("alice", RelationshipDesires.EnemyFear("bob", value: 30));

        Desire? realized = null;
        wf.FearRealized += (_, d) => realized = d;

        matrix.Get("alice", "bob").Flags.Add(RelationshipFlag.Enemy);
        wf.Evaluate(matrix, "alice");

        Assert.Equal(-30f, wf.MeterFor("alice").Score);
        Assert.NotNull(realized);
    }

    [Fact]
    public void Unmet_desire_stays_and_does_not_move_aspiration()
    {
        var matrix = new RelationshipMatrix();
        var wf = new WantsAndFearsSystem();
        wf.Add("alice", RelationshipDesires.BefriendWant("bob"));

        wf.Evaluate(matrix, "alice"); // sem amizade ainda

        Assert.Equal(0f, wf.MeterFor("alice").Score);
        Assert.Single(wf.DesiresOf("alice"));
    }

    [Fact]
    public void Unrequited_love_fear_realizes_only_when_not_reciprocated()
    {
        var matrix = new RelationshipMatrix();
        var wf = new WantsAndFearsSystem();
        wf.Add("alice", RelationshipDesires.UnrequitedLoveFear("bob", value: 20));

        // Alice ama Bob...
        matrix.Get("alice", "bob").Flags.Add(RelationshipFlag.Love);
        // ...e Bob também a ama de volta: o medo NÃO se realiza.
        matrix.Get("bob", "alice").Romance.ApplyLifetime(80f);
        wf.Evaluate(matrix, "alice");
        Assert.Equal(0f, wf.MeterFor("alice").Score);

        // Bob esfria (romance abaixo do limiar): agora o medo se concretiza.
        matrix.Get("bob", "alice").Romance.ApplyLifetime(-80f);
        wf.Evaluate(matrix, "alice");
        Assert.Equal(-20f, wf.MeterFor("alice").Score);
    }

    [Fact]
    public void Attaching_to_a_resolver_evaluates_after_interactions()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);
        var wf = new WantsAndFearsSystem();
        wf.AttachTo(resolver, matrix);
        wf.Add("alice", RelationshipDesires.BefriendWant("bob"));

        // Bob já gosta de Alice; Alice quase no limiar.
        matrix.Get("bob", "alice").Value.ApplyDaily(60f);
        matrix.Get("alice", "bob").Value.ApplyDaily(48f);

        // Conversar cruza o limiar mútuo de amizade -> Want cumprido.
        resolver.Perform("alice", "bob", InteractionLibrary.Talk);

        Assert.True(wf.MeterFor("alice").Score > 0f);
        Assert.Empty(wf.DesiresOf("alice"));
    }

    [Fact]
    public void Desire_with_non_positive_value_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Desire
        {
            Id = "x",
            Description = "x",
            Kind = DesireKind.Want,
            TargetId = "bob",
            AspirationValue = 0,
            IsMet = (_, _, _) => true,
        });
    }

    [Fact]
    public void Add_validates_owner_and_desire()
    {
        var wf = new WantsAndFearsSystem();

        Assert.Throws<ArgumentException>(() => wf.Add("  ", RelationshipDesires.BefriendWant("bob")));
        Assert.Throws<ArgumentNullException>(() => wf.Add("alice", null!));
    }

    [Fact]
    public void Evaluate_null_matrix_throws()
    {
        var wf = new WantsAndFearsSystem();
        Assert.Throws<ArgumentNullException>(() => wf.Evaluate(null!, "alice"));
    }
}
