namespace EconomySystem.Core.Businesses;

/// <summary>Um lote de estoque: itens comprados para revenda.</summary>
public sealed class StockItem
{
    public required string Id { get; init; }

    /// <summary>Custo unitário pago para adquirir (em $Money).</summary>
    public required int UnitCost { get; init; }

    /// <summary>Quantidade em prateleira.</summary>
    public int Quantity { get; set; }
}

/// <summary>
/// Negócio próprio (Open for Business): estoque, markup, funcionários, ranking
/// por fidelidade. O caixa do negócio é o próprio <see cref="HouseholdFunds"/>
/// do domicílio dono (TS2: dinheiro do negócio e da família é o mesmo bolso).
/// Perks por ranking ficam para fase posterior.
/// </summary>
public sealed class Business
{
    private readonly string _id = string.Empty;
    private readonly string _ownerId = string.Empty;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Business id required", nameof(Id))
            : value;
    }

    /// <summary>Personagem dono (chave do RelationshipMatrix).</summary>
    public required string OwnerId
    {
        get => _ownerId;
        init => _ownerId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("OwnerId required", nameof(OwnerId))
            : value;
    }

    /// <summary>Markup de venda sobre o custo, em %. Limitado por <see cref="BusinessRules"/>.</summary>
    public int MarkupPercent { get; set; } = BusinessRules.DefaultMarkupPercent;

    /// <summary>Estoque, indexado por Id do item.</summary>
    public Dictionary<string, StockItem> Stock { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Funcionários contratados.</summary>
    public List<BusinessEmployee> Employees { get; } = new();

    /// <summary>Fidelidade da clientela (estrelas).</summary>
    public CustomerLoyalty Loyalty { get; } = new();

    /// <summary>Ranking atual do negócio (estrelas de fidelidade).</summary>
    public int Rank => Loyalty.Stars;

    /// <summary>Perks desbloqueados pelo ranking atual (v3).</summary>
    public IReadOnlyList<BusinessPerk> UnlockedPerks => BusinessPerks.ForRank(Rank);

    /// <summary>Desconto de atacado na reposição, % (perk WholesaleDiscount).</summary>
    public int RestockDiscountPercent =>
        UnlockedPerks.Contains(BusinessPerk.WholesaleDiscount) ? BusinessPerks.WholesaleDiscountPercent : 0;

    /// <summary>Preço de venda de um item = custo × (1 + markup).</summary>
    public int SalePriceOf(StockItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.UnitCost * (100 + Math.Clamp(MarkupPercent, 0, BusinessRules.MaxMarkupPercent)) / 100;
    }
}
