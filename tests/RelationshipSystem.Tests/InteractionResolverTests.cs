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

    [Fact]
    public void Null_definition_throws()
    {
        var resolver = new InteractionResolver(new RelationshipMatrix());

        Assert.Throws<ArgumentNullException>(
            () => resolver.Perform("alice", "bob", null!));
    }

    [Fact]
    public void Rejected_interaction_applies_reject_effects()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        // Definição determinística que sempre rejeita.
        var alwaysReject = new InteractionDefinition
        {
            Id = "Test",
            DisplayName = "Test",
            IsRomantic = false,
            Available = _ => true,
            Accepted = _ => false,
            OnAccept = new InteractionEffect(DailyDelta: 99f, LifetimeDelta: 99f),
            OnReject = new InteractionEffect(DailyDelta: -7f, LifetimeDelta: -2f),
        };

        bool accepted = resolver.Perform("alice", "bob", alwaysReject);

        Assert.False(accepted);
        Assert.Equal(-7f, matrix.Get("alice", "bob").Value.Daily);
        Assert.Equal(-2f, matrix.Get("alice", "bob").Value.Lifetime);
    }

    [Fact]
    public void InteractionPerformed_event_reports_outcome()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        (string from, string to, string id, bool accepted)? captured = null;
        resolver.InteractionPerformed += (from, to, def, accepted) =>
            captured = (from, to, def.Id, accepted);

        resolver.Perform("alice", "bob", InteractionLibrary.Talk);

        Assert.NotNull(captured);
        Assert.Equal(("alice", "bob", "Talk", true), captured);
    }

    [Fact]
    public void Insult_drives_daily_below_enemy_threshold_and_raises_event()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        // Insult dá -15 daily; precisamos chegar a <= -50. Começamos em -40.
        matrix.Get("alice", "bob").Value.ApplyDaily(-40f);

        Relationship? enemies = null;
        resolver.BecameEnemies += rel => enemies = rel;

        resolver.Perform("alice", "bob", InteractionLibrary.Insult);

        var rel = matrix.Get("alice", "bob");
        Assert.True(rel.EffectiveDaily <= RelationshipThresholds.Enemy);
        Assert.NotNull(enemies);
        Assert.Contains(RelationshipFlag.Enemy, rel.Flags);
    }

    [Fact]
    public void BecameEnemies_fires_only_once_while_flag_persists()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);
        matrix.Get("alice", "bob").Value.ApplyDaily(-40f);

        int count = 0;
        resolver.BecameEnemies += _ => count++;

        resolver.Perform("alice", "bob", InteractionLibrary.Insult); // cruza -50
        resolver.Perform("alice", "bob", InteractionLibrary.Insult); // continua inimigo

        Assert.Equal(1, count);
    }

    [Fact]
    public void Romantic_interaction_forms_love_and_raises_event()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        // Flirt dá +3 lifetime; pré-carrega o lifetime logo abaixo de 70.
        matrix.Get("alice", "bob").Value.ApplyLifetime(68f);
        // Flirt só é aceito com EffectiveDaily > 60 (atração 0); usamos 62.
        matrix.Get("alice", "bob").Value.ApplyDaily(62f);

        Relationship? love = null;
        resolver.FellInLove += rel => love = rel;

        resolver.Perform("alice", "bob", InteractionLibrary.Flirt);

        var rel = matrix.Get("alice", "bob");
        Assert.True(rel.EffectiveLifetime >= RelationshipThresholds.Love);
        Assert.NotNull(love);
        Assert.Contains(RelationshipFlag.Love, rel.Flags);
    }

    [Fact]
    public void Love_breaks_and_raises_heartbroken_when_lifetime_drops()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);
        var rel = matrix.Get("alice", "bob");

        // Estado inicial: apaixonada.
        rel.Value.ApplyLifetime(72f);
        rel.Value.ApplyDaily(50f);
        rel.Flags.Add(RelationshipFlag.Love);

        Relationship? broken = null;
        resolver.HeartBroken += r => broken = r;

        // Insultar (não-romântico) derruba o lifetime em -5 -> 67 < 70.
        resolver.Perform("alice", "bob", InteractionLibrary.Insult);

        Assert.True(rel.EffectiveLifetime < RelationshipThresholds.Love);
        Assert.DoesNotContain(RelationshipFlag.Love, rel.Flags);
        Assert.NotNull(broken);
    }

    [Fact]
    public void Friendship_breaks_and_raises_event_when_daily_drops()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        // Ambos os lados amigos (>= 50).
        matrix.Get("alice", "bob").Value.ApplyDaily(52f);
        matrix.Get("bob", "alice").Value.ApplyDaily(60f);
        matrix.Get("alice", "bob").Flags.Add(RelationshipFlag.Friend);

        Relationship? broken = null;
        resolver.FriendshipBroken += rel => broken = rel;

        // Insulto (-15) joga alice->bob para 37 < 50, quebrando a amizade.
        resolver.Perform("alice", "bob", InteractionLibrary.Insult);

        Assert.False(matrix.AreFriends("alice", "bob"));
        Assert.DoesNotContain(RelationshipFlag.Friend, matrix.Get("alice", "bob").Flags);
        Assert.NotNull(broken);
    }

    [Fact]
    public void Crush_is_removed_when_daily_falls_below_threshold()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);
        var rel = matrix.Get("alice", "bob");

        // Forma o crush via Flirt (romântico, daily alto).
        rel.Value.ApplyDaily(69f);
        resolver.Perform("alice", "bob", InteractionLibrary.Flirt); // +8 -> 77 >= 70
        Assert.Contains(RelationshipFlag.Crush, rel.Flags);

        // Um insulto derruba o daily abaixo de 70; o crush não deve grudar.
        resolver.Perform("alice", "bob", InteractionLibrary.Insult); // -15 -> 62
        Assert.DoesNotContain(RelationshipFlag.Crush, rel.Flags);
    }

    // --- Tópicos de conversa (interesses) ---

    private static CharacterTraits Character(string id, int sportsLevel) => new()
    {
        Id = id,
        Name = id,
        Zodiac = Zodiac.Leo,
        Aspiration = Aspiration.Knowledge,
        Personality = new Personality(5, 5, 5, 5, 5),
        TurnOns = new[] { "A", "B" },
        TurnOff = "Lazy",
        Tags = Array.Empty<string>(),
        Interests = new Dictionary<string, int> { [InterestTopics.Sports] = sportsLevel },
    };

    [Fact]
    public void Shared_topic_boosts_daily_gain_of_a_conversation()
    {
        var matrix = new RelationshipMatrix();
        var registry = new CharacterRegistry();
        registry.Add(Character("alice", 10));
        registry.Add(Character("bob", 10));
        var resolver = new InteractionResolver(matrix, registry);

        resolver.Perform("alice", "bob", InteractionLibrary.Talk, InterestTopics.Sports);

        // Talk dá +3; tópico mútuo a 10 soma +5 * 0.6 = +3 -> 6.
        Assert.Equal(6f, matrix.Get("alice", "bob").Value.Daily, 3);
    }

    [Fact]
    public void Boring_topic_dampens_a_conversation()
    {
        var matrix = new RelationshipMatrix();
        var registry = new CharacterRegistry();
        registry.Add(Character("alice", 0));
        registry.Add(Character("bob", 0));
        var resolver = new InteractionResolver(matrix, registry);

        resolver.Perform("alice", "bob", InteractionLibrary.Talk, InterestTopics.Sports);

        // Talk +3; tópico que entedia ambos: -5 * 0.6 = -3 -> 0.
        Assert.Equal(0f, matrix.Get("alice", "bob").Value.Daily, 3);
    }

    [Fact]
    public void Topic_is_ignored_without_a_character_registry()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix); // sem registro

        resolver.Perform("alice", "bob", InteractionLibrary.Talk, InterestTopics.Sports);

        Assert.Equal(3f, matrix.Get("alice", "bob").Value.Daily);
    }

    // --- Sentimentos emergem dos marcos ---

    [Fact]
    public void Forming_friendship_creates_a_close_sentiment()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);
        matrix.Get("bob", "alice").Value.ApplyDaily(60f);
        matrix.Get("alice", "bob").Value.ApplyDaily(48f);

        resolver.Perform("alice", "bob", InteractionLibrary.Talk); // cruza 50

        Assert.Contains(matrix.Get("alice", "bob").Sentiments, s => s.Type == SentimentType.Close);
    }

    [Fact]
    public void Falling_in_love_creates_an_enamored_sentiment()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);
        matrix.Get("alice", "bob").Value.ApplyLifetime(68f);
        matrix.Get("alice", "bob").Value.ApplyDaily(62f);

        resolver.Perform("alice", "bob", InteractionLibrary.Flirt);

        Assert.Contains(matrix.Get("alice", "bob").Sentiments, s => s.Type == SentimentType.Enamored);
    }

    [Fact]
    public void Becoming_enemies_creates_a_bitter_sentiment()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);
        matrix.Get("alice", "bob").Value.ApplyDaily(-40f);

        resolver.Perform("alice", "bob", InteractionLibrary.Insult); // cruza -50

        Assert.Contains(matrix.Get("alice", "bob").Sentiments, s => s.Type == SentimentType.Bitter);
    }

    [Fact]
    public void Authored_sentiment_on_effect_is_applied_and_cloned()
    {
        var matrix = new RelationshipMatrix();
        var resolver = new InteractionResolver(matrix);

        resolver.Perform("alice", "bob", InteractionLibrary.GiveGift);

        var rel = matrix.Get("alice", "bob");
        var adoring = Assert.Single(rel.Sentiments, s => s.Type == SentimentType.Adoring);
        // Clonado do template: a instância no relacionamento não é a da definição.
        Assert.NotSame(InteractionLibrary.GiveGift.OnAccept.ResultingSentiment, adoring);
    }
}
