namespace EconomySystem.Core.Businesses;

/// <summary>
/// Perks desbloqueados conforme o ranking (estrelas) do negócio sobe — os
/// "business perks" do The Sims 2: Open for Business.
/// </summary>
public enum BusinessPerk
{
    /// <summary>Atacado mais barato: desconto na reposição de estoque.</summary>
    WholesaleDiscount,

    /// <summary>Treinamento de vendas avançado (descritivo).</summary>
    AdvancedSalesTraining,

    /// <summary>Programa de fidelidade: clientes recorrentes (descritivo).</summary>
    FrequentBuyerProgram,
}

/// <summary>
/// Regras de desbloqueio de perks por ranking. Centralizadas (como
/// <c>EconomyThresholds</c>), mas no namespace do negócio para evitar acoplar o
/// núcleo de constantes ao enum de perks.
/// </summary>
public static class BusinessPerks
{
    /// <summary>Desconto de atacado concedido pelo perk <see cref="BusinessPerk.WholesaleDiscount"/>, em %.</summary>
    public const int WholesaleDiscountPercent = 15;

    /// <summary>Perks desbloqueados para um dado ranking (0..5 estrelas).</summary>
    public static IReadOnlyList<BusinessPerk> ForRank(int rank)
    {
        var perks = new List<BusinessPerk>();
        if (rank >= 2) perks.Add(BusinessPerk.WholesaleDiscount);
        if (rank >= 3) perks.Add(BusinessPerk.AdvancedSalesTraining);
        if (rank >= 4) perks.Add(BusinessPerk.FrequentBuyerProgram);
        return perks;
    }
}
