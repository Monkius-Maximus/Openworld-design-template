using EconomySystem.Core;
using EconomySystem.Core.Careers;
using EconomySystem.Core.Integration;
using RelationshipSystem.Core;
using Xunit;

namespace EconomySystem.Tests;

public class CareerPromotionTests
{
    private static void MakeFriends(RelationshipMatrix m, string a, string b)
    {
        m.Get(a, b).Value.ApplyDaily(60f);
        m.Get(b, a).Value.ApplyDaily(60f);
    }

    private static CareerState BusinessRookie(string id) =>
        new() { CharacterId = id, Career = CareerLibrary.Business };

    // Nível 2 de Negócios exige: 1 amigo, Charisma 1, humor 30.
    private static Dictionary<string, int> CharismaOne() => new() { ["Charisma"] = 1 };

    [Fact]
    public void Promotion_succeeds_when_friends_skills_and_mood_met()
    {
        var matrix = new RelationshipMatrix();
        MakeFriends(matrix, "alice", "bob"); // 1 amigo
        var bridge = new RelationshipEconomyBridge(matrix);
        var resolver = new CareerResolver();
        var state = BusinessRookie("alice");

        bool promoted = bridge.EvaluatePromotion(resolver, state, CharismaOne(), mood: 40);

        Assert.True(promoted);
        Assert.Equal(2, state.CurrentLevel);
        Assert.Equal("Executivo Júnior", state.Title);
    }

    [Fact]
    public void Missing_friend_blocks_promotion()
    {
        var matrix = new RelationshipMatrix(); // sem amigos
        var bridge = new RelationshipEconomyBridge(matrix);
        var state = BusinessRookie("alice");

        bool promoted = bridge.EvaluatePromotion(new CareerResolver(), state, CharismaOne(), mood: 40);

        Assert.False(promoted);
        Assert.Equal(1, state.CurrentLevel);
    }

    [Fact]
    public void Missing_skill_blocks_promotion()
    {
        var matrix = new RelationshipMatrix();
        MakeFriends(matrix, "alice", "bob");
        var bridge = new RelationshipEconomyBridge(matrix);
        var state = BusinessRookie("alice");

        bool promoted = bridge.EvaluatePromotion(new CareerResolver(), state,
            new Dictionary<string, int>(), mood: 40); // sem Charisma

        Assert.False(promoted);
    }

    [Fact]
    public void Low_mood_blocks_promotion()
    {
        var matrix = new RelationshipMatrix();
        MakeFriends(matrix, "alice", "bob");
        var bridge = new RelationshipEconomyBridge(matrix);
        var state = BusinessRookie("alice");

        bool promoted = bridge.EvaluatePromotion(new CareerResolver(), state, CharismaOne(), mood: 10);

        Assert.False(promoted);
    }

    [Fact]
    public void Promotion_raises_event_with_new_level()
    {
        var matrix = new RelationshipMatrix();
        MakeFriends(matrix, "alice", "bob");
        var bridge = new RelationshipEconomyBridge(matrix);
        var resolver = new CareerResolver();
        CareerLevel? reached = null;
        resolver.Promoted += (_, level) => reached = level;

        bridge.EvaluatePromotion(resolver, BusinessRookie("alice"), CharismaOne(), mood: 40);

        Assert.NotNull(reached);
        Assert.Equal(2, reached!.Level);
    }

    [Fact]
    public void Top_level_cannot_be_promoted()
    {
        var solo = new CareerDefinition
        {
            Id = "solo",
            DisplayName = "Autônomo",
            Track = CareerTrack.Gig,
            Levels = new[] { new CareerLevel(1, "Único", 100, 0, new Dictionary<string, int>(), 0) },
        };
        var state = new CareerState { CharacterId = "alice", Career = solo };

        Assert.True(state.IsAtTop);
        Assert.False(state.EligibleForPromotion(new CareerContext(new Dictionary<string, int>(), 100, 99)));
        Assert.Throws<InvalidOperationException>(() => state.Promote());
    }

    [Fact]
    public void Career_with_non_contiguous_levels_throws()
    {
        Assert.Throws<ArgumentException>(() => new CareerDefinition
        {
            Id = "bad",
            DisplayName = "Ruim",
            Track = CareerTrack.Traditional,
            Levels = new[]
            {
                new CareerLevel(1, "A", 100, 0, new Dictionary<string, int>(), 0),
                new CareerLevel(3, "C", 200, 0, new Dictionary<string, int>(), 0), // pula o 2
            },
        });
    }
}
