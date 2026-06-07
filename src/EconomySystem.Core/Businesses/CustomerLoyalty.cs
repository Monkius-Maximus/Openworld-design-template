namespace EconomySystem.Core.Businesses;

/// <summary>
/// Fidelidade de clientes de um negócio, em estrelas de 1 a 5 (Open for
/// Business). Aqui a fidelidade deriva do valor que cada cliente já gastou;
/// na integração, ela co-evolui com o relacionamento dono↔cliente.
/// </summary>
public sealed class CustomerLoyalty
{
    private readonly Dictionary<string, int> _spendByCustomer = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registra gasto de um cliente.</summary>
    public void RecordSpend(string customerId, int amount)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("customerId required", nameof(customerId));
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "amount must be >= 0");

        _spendByCustomer.TryGetValue(customerId, out int current);
        _spendByCustomer[customerId] = current + amount;
    }

    /// <summary>Total gasto por um cliente.</summary>
    public int SpendOf(string customerId) =>
        _spendByCustomer.TryGetValue(customerId, out int v) ? v : 0;

    /// <summary>Total gasto por toda a clientela.</summary>
    public int TotalSpend => _spendByCustomer.Values.Sum();

    /// <summary>Estrelas (0..5) do negócio, derivadas do gasto total acumulado.</summary>
    public int Stars => StarsFor(TotalSpend);

    /// <summary>Converte um valor acumulado em estrelas via thresholds.</summary>
    public static int StarsFor(int totalSpend)
    {
        var thresholds = EconomyThresholds.LoyaltyStarThresholds;
        int stars = 0;
        for (int i = 0; i < thresholds.Count; i++)
        {
            if (totalSpend >= thresholds[i])
                stars = i + 1;
        }
        return stars;
    }
}
