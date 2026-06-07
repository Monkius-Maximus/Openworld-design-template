using RelationshipSystem.Core;
using Xunit;

namespace RelationshipSystem.Tests;

public class InterestCalculatorTests
{
    private static CharacterTraits With(IReadOnlyDictionary<string, int>? interests) =>
        new()
        {
            Id = "c",
            Name = "C",
            Zodiac = Zodiac.Leo,
            Aspiration = Aspiration.Knowledge,
            Personality = new Personality(5, 5, 5, 5, 5),
            TurnOns = new[] { "A", "B" },
            TurnOff = "Lazy",
            Tags = Array.Empty<string>(),
            Interests = interests ?? new Dictionary<string, int>(),
        };

    [Fact]
    public void Shared_interest_is_positive_when_both_are_keen()
    {
        var a = With(new Dictionary<string, int> { [InterestTopics.Sports] = 10 });
        var b = With(new Dictionary<string, int> { [InterestTopics.Sports] = 10 });

        Assert.Equal(5f, InterestCalculator.SharedInterest(a, b, InterestTopics.Sports));
    }

    [Fact]
    public void Shared_interest_is_negative_when_both_are_bored()
    {
        var a = With(new Dictionary<string, int> { [InterestTopics.Politics] = 0 });
        var b = With(new Dictionary<string, int> { [InterestTopics.Politics] = 0 });

        Assert.Equal(-5f, InterestCalculator.SharedInterest(a, b, InterestTopics.Politics));
    }

    [Fact]
    public void Missing_topic_counts_as_zero_interest()
    {
        var a = With(new Dictionary<string, int> { [InterestTopics.Food] = 10 });
        var b = With(null); // não tem o tópico

        // média (10 + 0)/2 = 5 → exatamente neutro.
        Assert.Equal(0f, InterestCalculator.SharedInterest(a, b, InterestTopics.Food));
    }

    [Fact]
    public void Topic_lookup_is_case_insensitive()
    {
        var a = With(new Dictionary<string, int> { ["Sports"] = 10 });
        var b = With(new Dictionary<string, int> { ["sports"] = 10 });

        Assert.Equal(5f, InterestCalculator.SharedInterest(a, b, "SPORTS"));
    }

    [Fact]
    public void Conversation_modifier_scales_shared_interest_by_weight()
    {
        var a = With(new Dictionary<string, int> { [InterestTopics.Culture] = 10 });
        var b = With(new Dictionary<string, int> { [InterestTopics.Culture] = 10 });

        Assert.Equal(5f * InterestWeights.ConversationBonusPerPoint,
            InterestCalculator.ConversationModifier(a, b, InterestTopics.Culture));
    }

    [Fact]
    public void Blank_topic_throws()
    {
        var c = With(null);
        Assert.Throws<ArgumentException>(() => InterestCalculator.SharedInterest(c, c, "  "));
    }

    [Fact]
    public void Interest_level_out_of_range_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => With(new Dictionary<string, int> { ["X"] = 11 }));
    }

    [Fact]
    public void Blank_interest_topic_throws()
    {
        Assert.Throws<ArgumentException>(
            () => With(new Dictionary<string, int> { ["  "] = 5 }));
    }

    [Fact]
    public void Interests_default_to_empty()
    {
        Assert.Empty(With(null).Interests);
    }
}
