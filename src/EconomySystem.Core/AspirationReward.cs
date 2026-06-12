namespace EconomySystem.Core;

/// <summary>
/// Área da vida que um objeto de recompensa de aspiração beneficia (The Sims 2:
/// recompensas ajudam necessidades, habilidades, social ou geram dinheiro).
/// </summary>
public enum RewardBenefit
{
    /// <summary>Necessidades/humor (ex.: Energizador).</summary>
    Motives,

    /// <summary>Aprendizado de habilidades (ex.: Cérebro em Conserva).</summary>
    Skill,

    /// <summary>Relacionamentos (ex.: Espelho do Amor).</summary>
    Social,

    /// <summary>Gera $Money (ex.: Árvore do Dinheiro).</summary>
    Money,
}

/// <summary>
/// Objeto de recompensa comprável SOMENTE com pontos de aspiração (não com
/// $Money). Definição imutável validada no <c>init</c>.
/// </summary>
public sealed class AspirationReward
{
    private readonly string _id = string.Empty;
    private readonly int _pointCost;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Reward id required", nameof(Id))
            : value;
    }

    public required string Name { get; init; }

    /// <summary>Custo em pontos de aspiração. Deve ser &gt; 0.</summary>
    public required int PointCost
    {
        get => _pointCost;
        init => _pointCost = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(PointCost), value, "PointCost must be > 0");
    }

    /// <summary>Área beneficiada (descritivo; o efeito de jogo é do consumidor).</summary>
    public required RewardBenefit Benefit { get; init; }
}
