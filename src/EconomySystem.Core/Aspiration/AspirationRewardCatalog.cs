namespace EconomySystem.Core.Aspiration;

/// <summary>
/// Catálogo de objetos de recompensa de aspiração prontos (estilo The Sims 2),
/// indexado por Id. Espelha o <c>InteractionLibrary</c>. Inclui a clássica
/// "Árvore do Dinheiro", que liga a moeda soft de volta ao caixa.
/// </summary>
public static class AspirationRewardCatalog
{
    /// <summary>Energizador: recarrega necessidades/humor.</summary>
    public static readonly AspirationReward EnergyBoost = new()
    {
        Id = "EnergyBoost",
        Name = "Energizador",
        PointCost = 5_000,
        Benefit = RewardBenefit.Motives,
    };

    /// <summary>Cérebro em Conserva: acelera o aprendizado de habilidades.</summary>
    public static readonly AspirationReward ThinkingCap = new()
    {
        Id = "ThinkingCap",
        Name = "Cérebro em Conserva",
        PointCost = 8_000,
        Benefit = RewardBenefit.Skill,
    };

    /// <summary>Espelho do Amor: ajuda nas relações.</summary>
    public static readonly AspirationReward LoveMirror = new()
    {
        Id = "LoveMirror",
        Name = "Espelho do Amor",
        PointCost = 10_000,
        Benefit = RewardBenefit.Social,
    };

    /// <summary>Árvore do Dinheiro: gera Simoleons ao longo do tempo.</summary>
    public static readonly AspirationReward MoneyTree = new()
    {
        Id = "MoneyTree",
        Name = "Árvore do Dinheiro",
        PointCost = 15_000,
        Benefit = RewardBenefit.Money,
    };

    /// <summary>Todas as recompensas registradas, indexadas por Id.</summary>
    public static readonly IReadOnlyDictionary<string, AspirationReward> All =
        new[] { EnergyBoost, ThinkingCap, LoveMirror, MoneyTree }
            .ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
}
