namespace EconomySystem.Core;

/// <summary>
/// Efeito de um desfecho de chance card sobre a carreira.
/// </summary>
public enum CareerEffect
{
    None,
    Promote,
    Demote,
}

/// <summary>
/// Um desfecho possível de um chance card: rótulo, impacto no caixa (assinado) e
/// efeito na carreira.
/// </summary>
public sealed record ChanceCardOutcome(string Label, int FundsDelta, CareerEffect Effect);

/// <summary>
/// Evento aleatório de trabalho (chance card). O The Sims 2 tinha desfechos
/// pré-determinados; aqui damos uma visão moderna oferecendo DUAS opções
/// (como os jogos seguintes) — o jogador escolhe e arca com as consequências.
/// </summary>
public sealed class ChanceCard
{
    private readonly string _id = string.Empty;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("ChanceCard id required", nameof(Id))
            : value;
    }

    /// <summary>Carreira a que o card pertence.</summary>
    public required string CareerId { get; init; }

    /// <summary>Texto da situação apresentada ao jogador.</summary>
    public required string Prompt { get; init; }

    /// <summary>Primeira opção.</summary>
    public required ChanceCardOutcome OptionA { get; init; }

    /// <summary>Segunda opção.</summary>
    public required ChanceCardOutcome OptionB { get; init; }

    /// <summary>Nível em que o card pode aparecer (null = qualquer nível).</summary>
    public int? AtLevel { get; init; }
}
