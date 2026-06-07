using RelationshipSystem.Core;
using RelationshipSystem.Core.Interactions;

// Criar dois personagens.
var alice = new CharacterTraits
{
    Id = "alice",
    Name = "Alice",
    Zodiac = Zodiac.Leo,
    Aspiration = Aspiration.Romance,
    Personality = new Personality(Neat: 7, Outgoing: 8, Active: 6, Playful: 9, Nice: 8),
    TurnOns = new[] { "Fitness", "Blond" },
    TurnOff = "Lazy",
    Tags = new[] { "Blond", "Fitness", "Funny" },
    Interests = new Dictionary<string, int>
    {
        [InterestTopics.Sports] = 9,
        [InterestTopics.Culture] = 7,
        [InterestTopics.Politics] = 2,
    }
};

var bob = new CharacterTraits
{
    Id = "bob",
    Name = "Bob",
    Zodiac = Zodiac.Sagittarius,
    Aspiration = Aspiration.Romance,
    Personality = new Personality(Neat: 5, Outgoing: 9, Active: 8, Playful: 7, Nice: 6),
    TurnOns = new[] { "Blond", "Active" },
    TurnOff = "Neat",
    Tags = new[] { "Blond", "Active", "Charming" },
    Interests = new Dictionary<string, int>
    {
        [InterestTopics.Sports] = 10,
        [InterestTopics.Culture] = 1,
        [InterestTopics.Politics] = 8,
    }
};

// Registro de personagens: dá ao resolver acesso aos interesses para
// modular conversas por tópico.
var registry = new CharacterRegistry();
registry.Add(alice);
registry.Add(bob);

// Matriz, resolver e decay.
var matrix = new RelationshipMatrix();
var resolver = new InteractionResolver(matrix, registry);
var decay = new RelationshipDecaySystem();

// Atração assimétrica.
matrix.Get("alice", "bob").AttractionScore = AttractionCalculator.Calculate(alice, bob);
matrix.Get("bob", "alice").AttractionScore = AttractionCalculator.Calculate(bob, alice);

// Assinar eventos.
resolver.FriendshipFormed += rel => Console.WriteLine($"✓ Amizade: {rel.FromId} → {rel.ToId}");
resolver.FellInLove += rel => Console.WriteLine($"❤ Amor: {rel.FromId} → {rel.ToId}");
resolver.BecameEnemies += rel => Console.WriteLine($"\U0001F494 Inimigos: {rel.FromId} → {rel.ToId}");

Console.WriteLine($"Atração Alice→Bob: {matrix.Get("alice", "bob").AttractionScore}");
Console.WriteLine($"Atração Bob→Alice: {matrix.Get("bob", "alice").AttractionScore}");
Console.WriteLine($"Chemistry: {AttractionCalculator.Chemistry(alice, bob)}");

// Simular interações.
// Conversa sobre Esportes: ambos curtem muito (9 e 10) → ganho de Daily turbinado.
resolver.Perform("alice", "bob", InteractionLibrary.Talk, InterestTopics.Sports);
// Conversa sobre Cultura: Bob não liga (1) → conversa morna.
resolver.Perform("alice", "bob", InteractionLibrary.Talk, InterestTopics.Culture);
resolver.Perform("bob", "alice", InteractionLibrary.Compliment);
resolver.Perform("alice", "bob", InteractionLibrary.GiveGift); // gera sentimento "Adoring"

// Um dia passa.
decay.DailyTick(matrix);

// Três normalizações por dia.
for (int i = 0; i < RelationshipPhysics.NormalizationTicksPerDay; i++)
    decay.NormalizationTick(matrix);

var ab = matrix.Get("alice", "bob");
Console.WriteLine($"Alice → Bob: daily={ab.Value.Daily}, lifetime={ab.Value.Lifetime}, effective={ab.EffectiveDaily}");

// Sentimentos persistentes acumulados (estilo The Sims 4).
Console.WriteLine($"Sentimentos de Alice por Bob ({ab.Sentiments.Count}/{SentimentDefaults.MaxSentiments}):");
foreach (var s in ab.Sentiments)
    Console.WriteLine($"  - {s.Type} ({s.Polarity}), intensidade {s.Intensity}, {(s.IsLongTerm ? "permanente" : $"{s.RemainingHours}h")}");
