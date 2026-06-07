using RelationshipSystem.Core;
using Xunit;

namespace RelationshipSystem.Tests;

public class SentimentTests
{
    private static Relationship Rel() => new RelationshipMatrix().Get("a", "b");

    [Theory]
    [InlineData(SentimentType.Close, SentimentPolarity.Positive)]
    [InlineData(SentimentType.Enamored, SentimentPolarity.Positive)]
    [InlineData(SentimentType.Bitter, SentimentPolarity.Negative)]
    [InlineData(SentimentType.Resentful, SentimentPolarity.Negative)]
    public void Polarity_follows_type(SentimentType type, SentimentPolarity expected)
    {
        var s = new Sentiment { Type = type };
        Assert.Equal(expected, s.Polarity);
    }

    [Fact]
    public void Long_term_sentiment_never_expires_with_time()
    {
        var s = new Sentiment { Type = SentimentType.Enamored, IsLongTerm = true };

        bool expired = s.DecayTime(10_000f);

        Assert.False(expired);
        Assert.False(s.IsExpired);
    }

    [Fact]
    public void Short_term_sentiment_expires_when_time_runs_out()
    {
        var s = new Sentiment { Type = SentimentType.Adoring, RemainingHours = 24f };

        Assert.False(s.DecayTime(12f));
        Assert.True(s.DecayTime(12f));
        Assert.True(s.IsExpired);
    }

    [Fact]
    public void Negative_hours_throw()
    {
        var s = new Sentiment { Type = SentimentType.Close };
        Assert.Throws<ArgumentOutOfRangeException>(() => s.DecayTime(-1f));
    }

    [Fact]
    public void Adding_sentiment_stores_it()
    {
        var rel = Rel();
        rel.AddSentiment(new Sentiment { Type = SentimentType.Close, IsLongTerm = true });

        Assert.Single(rel.Sentiments);
        Assert.Equal(SentimentType.Close, rel.Sentiments[0].Type);
    }

    [Fact]
    public void Same_type_reinforces_instead_of_duplicating()
    {
        var rel = Rel();
        rel.AddSentiment(new Sentiment { Type = SentimentType.Adoring, Intensity = 1f, RemainingHours = 24f });
        rel.AddSentiment(new Sentiment { Type = SentimentType.Adoring, Intensity = 2f, RemainingHours = 48f });

        Assert.Single(rel.Sentiments);
        Assert.Equal(3f, rel.Sentiments[0].Intensity);     // intensidade acumula
        Assert.Equal(48f, rel.Sentiments[0].RemainingHours); // prazo é estendido (máximo)
    }

    [Fact]
    public void Reinforcement_is_capped_at_max_intensity()
    {
        var rel = Rel();
        rel.AddSentiment(new Sentiment { Type = SentimentType.Bitter, Intensity = 4f, IsLongTerm = true });
        rel.AddSentiment(new Sentiment { Type = SentimentType.Bitter, Intensity = 4f, IsLongTerm = true });

        Assert.Equal(SentimentDefaults.MaxIntensity, rel.Sentiments[0].Intensity);
    }

    [Fact]
    public void Fifth_sentiment_evicts_the_weakest()
    {
        var rel = Rel();
        rel.AddSentiment(new Sentiment { Type = SentimentType.Close, Intensity = 5f, IsLongTerm = true });
        rel.AddSentiment(new Sentiment { Type = SentimentType.Adoring, Intensity = 4f, IsLongTerm = true });
        rel.AddSentiment(new Sentiment { Type = SentimentType.Enamored, Intensity = 3f, IsLongTerm = true });
        rel.AddSentiment(new Sentiment { Type = SentimentType.Motivated, Intensity = 1f, IsLongTerm = true }); // mais fraco
        rel.AddSentiment(new Sentiment { Type = SentimentType.Bitter, Intensity = 2f, IsLongTerm = true });

        Assert.Equal(SentimentDefaults.MaxSentiments, rel.Sentiments.Count);
        Assert.DoesNotContain(rel.Sentiments, s => s.Type == SentimentType.Motivated);
    }

    [Fact]
    public void Decay_removes_expired_short_term_but_keeps_long_term()
    {
        var rel = Rel();
        rel.AddSentiment(new Sentiment { Type = SentimentType.Enamored, IsLongTerm = true });
        rel.AddSentiment(new Sentiment { Type = SentimentType.Adoring, RemainingHours = 12f });

        rel.DecaySentiments(24f);

        Assert.Single(rel.Sentiments);
        Assert.Equal(SentimentType.Enamored, rel.Sentiments[0].Type);
    }

    [Fact]
    public void Adding_null_sentiment_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Rel().AddSentiment(null!));
    }
}
