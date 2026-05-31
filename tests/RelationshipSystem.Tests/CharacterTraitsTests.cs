using RelationshipSystem.Core;
using Xunit;

namespace RelationshipSystem.Tests;

public class CharacterTraitsTests
{
    private static CharacterTraits Build(
        IReadOnlyList<string>? turnOns = null,
        string turnOff = "Lazy",
        IReadOnlyList<string>? tags = null,
        Personality? personality = null) =>
        new()
        {
            Id = "alice",
            Name = "Alice",
            Zodiac = Zodiac.Leo,
            Aspiration = Aspiration.Romance,
            Personality = personality ?? new Personality(7, 8, 6, 9, 8),
            TurnOns = turnOns ?? new[] { "Fitness", "Blond" },
            TurnOff = turnOff,
            Tags = tags ?? new[] { "Blond", "Funny" },
        };

    [Fact]
    public void Valid_traits_are_constructed()
    {
        var traits = Build();

        Assert.Equal("alice", traits.Id);
        Assert.Equal(2, traits.TurnOns.Count);
        Assert.Equal("Lazy", traits.TurnOff);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Wrong_number_of_turn_ons_throws(int count)
    {
        var turnOns = Enumerable.Range(0, count).Select(i => $"t{i}").ToArray();

        Assert.Throws<ArgumentException>(() => Build(turnOns: turnOns));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Blank_turn_off_throws(string? turnOff)
    {
        Assert.Throws<ArgumentException>(() => Build(turnOff: turnOff!));
    }

    [Fact]
    public void Blank_turn_on_throws()
    {
        Assert.Throws<ArgumentException>(() => Build(turnOns: new[] { "Fitness", "  " }));
    }

    [Fact]
    public void Blank_tag_throws()
    {
        Assert.Throws<ArgumentException>(() => Build(tags: new[] { "Blond", "" }));
    }

    [Fact]
    public void Empty_tags_are_allowed()
    {
        var traits = Build(tags: Array.Empty<string>());

        Assert.Empty(traits.Tags);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Personality_out_of_range_throws(int badValue)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Build(personality: new Personality(badValue, 5, 5, 5, 5)));
    }

    [Fact]
    public void Turn_ons_are_defensively_copied()
    {
        var source = new List<string> { "Fitness", "Blond" };
        var traits = Build(turnOns: source);

        source[0] = "Mutated";

        Assert.Equal("Fitness", traits.TurnOns[0]);
    }
}
