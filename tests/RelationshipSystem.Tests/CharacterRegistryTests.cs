using RelationshipSystem.Core;
using Xunit;

namespace RelationshipSystem.Tests;

public class CharacterRegistryTests
{
    private static CharacterTraits Make(string id) => new()
    {
        Id = id,
        Name = id,
        Zodiac = Zodiac.Leo,
        Aspiration = Aspiration.Knowledge,
        Personality = new Personality(5, 5, 5, 5, 5),
        TurnOns = new[] { "A", "B" },
        TurnOff = "Lazy",
        Tags = Array.Empty<string>(),
    };

    [Fact]
    public void Add_then_get_returns_the_character()
    {
        var reg = new CharacterRegistry();
        reg.Add(Make("alice"));

        Assert.Equal("alice", reg.Get("alice").Id);
        Assert.Equal(1, reg.Count);
    }

    [Fact]
    public void Lookup_is_case_insensitive()
    {
        var reg = new CharacterRegistry();
        reg.Add(Make("Alice"));

        Assert.True(reg.TryGet("alice", out var c));
        Assert.Equal("Alice", c.Id);
    }

    [Fact]
    public void Get_unknown_throws()
    {
        Assert.Throws<KeyNotFoundException>(() => new CharacterRegistry().Get("ghost"));
    }

    [Fact]
    public void Add_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CharacterRegistry().Add(null!));
    }
}
