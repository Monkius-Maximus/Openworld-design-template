namespace EconomySystem.Core.Businesses;

/// <summary>
/// Opera um negócio: repõe estoque (compra) e vende a clientes com markup.
/// Espelha o <c>InteractionResolver</c> (falha rápido + eventos). A interação
/// com relacionamentos (cliente = relacionamento) fica no
/// <c>RelationshipEconomyBridge</c>, mantendo este resolver desacoplado do
/// núcleo de relacionamentos.
/// </summary>
public sealed class BusinessResolver
{
    /// <summary>Disparado a cada venda concluída (dono, cliente, lucro).</summary>
    public event SaleEventHandler? Sold;

    /// <summary>
    /// Repõe o estoque comprando <paramref name="quantity"/> unidades a
    /// <paramref name="unitCost"/>. Debita o caixa do dono. Retorna false se
    /// faltar saldo (estoque não muda).
    /// </summary>
    public bool Restock(Business business, HouseholdFunds funds, string itemId, int unitCost, int quantity)
    {
        ArgumentNullException.ThrowIfNull(business);
        ArgumentNullException.ThrowIfNull(funds);
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("itemId required", nameof(itemId));
        if (unitCost < 0)
            throw new ArgumentOutOfRangeException(nameof(unitCost), unitCost, "unitCost must be >= 0");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "quantity must be > 0");

        int cost = unitCost * quantity;
        var tx = new MoneyTransaction
        {
            Reason = $"Reposição de estoque: {itemId}",
            Amount = -cost,
            Kind = TransactionKind.Restock,
        };

        if (!funds.TryWithdraw(tx))
            return false;

        if (business.Stock.TryGetValue(itemId, out var existing))
            existing.Quantity += quantity;
        else
            business.Stock[itemId] = new StockItem { Id = itemId, UnitCost = unitCost, Quantity = quantity };

        return true;
    }

    /// <summary>
    /// Vende uma unidade de <paramref name="itemId"/> ao cliente, SE
    /// <paramref name="customerBuys"/> for true (a decisão de compra é fornecida
    /// pelo chamador — tipicamente a partir do relacionamento, via bridge).
    /// Credita o lucro no caixa, baixa o estoque, registra fidelidade e emite
    /// <see cref="Sold"/>. Falha rápido se não houver estoque. Retorna o lucro
    /// (0 se o cliente não comprou).
    /// </summary>
    public int SellTo(Business business, HouseholdFunds funds, string customerId, string itemId, bool customerBuys)
    {
        ArgumentNullException.ThrowIfNull(business);
        ArgumentNullException.ThrowIfNull(funds);
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("customerId required", nameof(customerId));

        if (!business.Stock.TryGetValue(itemId, out var item) || item.Quantity <= 0)
            throw new InvalidOperationException($"Sem estoque de '{itemId}' no negócio '{business.Id}'.");

        if (!customerBuys)
            return 0;

        int price = business.SalePriceOf(item);
        int profit = price - item.UnitCost;

        item.Quantity--;
        funds.Deposit(new MoneyTransaction
        {
            Reason = $"Venda: {itemId}",
            Amount = price,
            Kind = TransactionKind.Sale,
        });
        business.Loyalty.RecordSpend(customerId, price);

        Sold?.Invoke(business.OwnerId, customerId, profit);
        return profit;
    }
}
