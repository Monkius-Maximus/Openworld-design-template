using RelationshipSystem.Core;
using Xunit;

namespace RelationshipSystem.Tests;

public class AttractionCalculatorTests
{
    private static CharacterTraits Make(
        string id,
        Zodiac zodiac = Zodiac.Leo,
        Aspiration aspiration = Aspiration.Romance,
        Personality? personality = null,
        IReadOnlyList<string>? turnOns = null,
        string turnOff = "Lazy",
        IReadOnlyList<string>? tags = null) =>
        new()
        {
            Id = id,
            Name = id,
            Zodiac = zodiac,
            Aspiration = aspiration,
            Personality = personality ?? new Personality(5, 5, 5, 5, 5),
            TurnOns = turnOns ?? new[] { "Fitness", "Blond" },
            TurnOff = turnOff,
            Tags = tags ?? Array.Empty<string>(),
        };

    [Fact]
    public void Turn_on_match_adds_weight()
    {
        var a = Make("a", turnOns: new[] { "Fitness", "Blond" }, tags: Array.Empty<string>());
        var b = Make("b", tags: new[] { "Fitness" });

        // Mesmo signo (Leo) => 0 zodiac; aspiração igual => +35; turn-on => +20;
        // personalidade idêntica => 25. Sem turn-off.
        int score = AttractionCalculator.Calculate(a, b);

        Assert.Equal(AttractionWeights.TurnOnWeight + AttractionWeights.SameAspirationBonus + 25, score);
    }

    [Fact]
    public void Turn_off_match_subtracts_double()
    {
        var a = Make("a", turnOff: "Messy", tags: Array.Empty<string>());
        var b = Make("b", tags: new[] { "Messy" });

        int withTurnOff = AttractionCalculator.Calculate(a, b);
        var bClean = Make("b", tags: Array.Empty<string>());
        int without = AttractionCalculator.Calculate(a, bClean);

        Assert.Equal(without - AttractionWeights.TurnOffWeight, withTurnOff);
    }

    [Fact]
    public void Attraction_is_asymmetric()
    {
        var a = Make("a", turnOns: new[] { "Blond", "Tall" }, tags: new[] { "Shy" });
        var b = Make("b", turnOns: new[] { "Shy", "Funny" }, tags: new[] { "Blond" });

        int aToB = AttractionCalculator.Calculate(a, b); // b é Blond -> +20
        int bToA = AttractionCalculator.Calculate(b, a); // a é Shy -> +20 (mas signos/asp iguais)

        // As direções não precisam coincidir; ao menos uma combinação difere.
        Assert.True(aToB != bToA || a.Tags.Count != b.Tags.Count);
    }

    [Fact]
    public void Same_zodiac_has_no_self_bonus()
    {
        Assert.Equal(0, ZodiacCompatibilityTable.Get(Zodiac.Leo, Zodiac.Leo));
    }

    [Fact]
    public void Same_element_is_max_positive()
    {
        // Aries e Leo são ambos de Fogo.
        Assert.Equal(AttractionWeights.ZodiacCompatibilityMax,
            ZodiacCompatibilityTable.Get(Zodiac.Aries, Zodiac.Leo));
    }

    [Fact]
    public void Chemistry_is_average_of_both_directions()
    {
        var a = Make("a");
        var b = Make("b");

        int expected = (AttractionCalculator.Calculate(a, b) + AttractionCalculator.Calculate(b, a)) / 2;

        Assert.Equal(expected, AttractionCalculator.Chemistry(a, b));
    }
}
