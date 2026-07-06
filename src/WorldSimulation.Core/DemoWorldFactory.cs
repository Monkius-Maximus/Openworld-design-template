using EconomySystem.Core;
using EconomySystem.Core.Careers;
using EconomySystem.Core.Market;
using RelationshipSystem.Core;

namespace WorldSimulation.Core;

/// <summary>
/// Monta o mundo de demonstração usado pela cena 3D e pelos testes: o
/// jogador (com carreira gig e caixa inicial) e dois vizinhos com traços
/// contrastantes — Alice (afável, com atração mútua) e Bruno (rabugento).
/// Conteúdo, como o InteractionLibrary/CareerLibrary, mora no núcleo.
/// </summary>
public static class DemoWorldFactory
{
    public const string PlayerId = "player";
    public const string AliceId = "alice";
    public const string BrunoId = "bruno";
    public const string PlayerHomeId = "casa-jogador";
    public const string NeighborsHomeId = "casa-vizinhos";

    public static GameWorld Create(CurrencyMarket? market = null)
    {
        var world = new GameWorld(market);

        var playerHome = new Household
        {
            Id = PlayerHomeId,
            Funds = new HouseholdFunds(WorldThresholds.DemoStartingFunds),
        };
        var neighborsHome = new Household
        {
            Id = NeighborsHomeId,
            Funds = new HouseholdFunds(WorldThresholds.DemoNeighborFunds),
        };
        world.AddHousehold(playerHome);
        world.AddHousehold(neighborsHome);

        world.AddCharacter(new CharacterTraits
        {
            Id = PlayerId,
            Name = "Jogador",
            Zodiac = Zodiac.Leo,
            Aspiration = Aspiration.Popularity,
            Personality = new Personality(Neat: 5, Outgoing: 7, Active: 6, Playful: 5, Nice: 7),
            TurnOns = new[] { "Music", "Fitness" },
            TurnOff = "Smoking",
            Tags = new[] { "Fitness" },
            Interests = new Dictionary<string, int> { ["Música"] = 8, ["Esportes"] = 7 },
        }, PlayerHomeId);

        world.AddCharacter(new CharacterTraits
        {
            Id = AliceId,
            Name = "Alice",
            Zodiac = Zodiac.Libra,
            Aspiration = Aspiration.Romance,
            Personality = new Personality(Neat: 6, Outgoing: 8, Active: 5, Playful: 7, Nice: 8),
            TurnOns = new[] { "Fitness", "Cologne" },
            TurnOff = "Stink",
            Tags = new[] { "Music", "Blond" },
            Interests = new Dictionary<string, int> { ["Música"] = 9, ["Culinária"] = 6 },
        }, NeighborsHomeId);

        world.AddCharacter(new CharacterTraits
        {
            Id = BrunoId,
            Name = "Bruno",
            Zodiac = Zodiac.Scorpio,
            Aspiration = Aspiration.Wealth,
            Personality = new Personality(Neat: 4, Outgoing: 3, Active: 4, Playful: 2, Nice: 2),
            TurnOns = new[] { "Money", "Cars" },
            TurnOff = "Laziness",
            Tags = new[] { "Grumpy" },
            Interests = new Dictionary<string, int> { ["Dinheiro"] = 9, ["Esportes"] = 2 },
        }, NeighborsHomeId);

        // Carreira gig do jogador: o salário diário movimenta o caixa no tick.
        playerHome.Careers[PlayerId] = new CareerState
        {
            CharacterId = PlayerId,
            Career = CareerLibrary.GigCourier,
        };

        // Wants & fears iniciais (avaliados automaticamente após cada interação).
        world.WantsAndFears.Add(PlayerId, RelationshipDesires.BefriendWant(AliceId));
        world.WantsAndFears.Add(PlayerId, RelationshipDesires.EnemyFear(BrunoId));
        world.WantsAndFears.Add(AliceId, RelationshipDesires.BefriendWant(PlayerId));

        // Atração inicial (facilita o flerte com Alice; atalho de demo — num
        // jogo completo, use o AttractionCalculator sobre os traços).
        world.Relationships.Get(PlayerId, AliceId).AttractionScore = 40;
        world.Relationships.Get(AliceId, PlayerId).AttractionScore = 35;

        return world;
    }
}
