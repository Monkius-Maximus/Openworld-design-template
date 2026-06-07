namespace EconomySystem.Core;

/// <summary>
/// Pista de carreira. Além da tradicional (escada 9-às-17 do The Sims 2), o
/// mundo moderno traz trabalho remoto e gig economy (fase v2).
/// </summary>
public enum CareerTrack
{
    /// <summary>Escada tradicional de promoções (estilo TS2).</summary>
    Traditional,

    /// <summary>Trabalho remoto (mesma escada, sem deslocamento).</summary>
    Remote,

    /// <summary>Gig economy: bicos por app, sem escada fixa.</summary>
    Gig,
}

/// <summary>
/// Um nível de uma carreira: cargo, salário diário e os requisitos para ser
/// promovido A ESTE nível (habilidades + amigos + humor — exatamente os gates
/// do The Sims 2). O número de amigos vem do <c>RelationshipMatrix</c> via
/// <c>RelationshipEconomyBridge.CountFriends</c>.
/// </summary>
public sealed record CareerLevel(
    int Level,
    string Title,
    int DailyWage,
    int RequiredFriends,
    IReadOnlyDictionary<string, int> RequiredSkills,
    int RequiredMood);

/// <summary>
/// Contexto avaliado numa tentativa de promoção: as habilidades atuais do
/// personagem (nome → nível 0..10), o humor e a contagem de amigos (injetada
/// pela integração com relacionamentos).
/// </summary>
public readonly record struct CareerContext(
    IReadOnlyDictionary<string, int> Skills,
    int Mood,
    int FriendCount);
