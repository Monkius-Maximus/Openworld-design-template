namespace EconomySystem.Core;

/// <summary>
/// Um bem do domicílio que deprecia com o tempo (estilo The Sims 2: o valor do
/// objeto cai do preço de compra rumo a um piso de revenda). A mecânica é o
/// análogo direto de <c>RelationshipValue.DecayDailyTowardZero</c> — só que o
/// alvo é um piso, não zero.
/// </summary>
public sealed class OwnedObject
{
    private readonly string _id = string.Empty;
    private readonly int _purchasePrice;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("OwnedObject id required", nameof(Id))
            : value;
    }

    /// <summary>Preço pago na compra (em $Money).</summary>
    public required int PurchasePrice
    {
        get => _purchasePrice;
        init
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(PurchasePrice), value, "PurchasePrice must be >= 0");
            _purchasePrice = value;
            CurrentValue = value; // começa valendo o preço de compra
        }
    }

    /// <summary>Se o objeto entra no cálculo da conta (TS2: quadros/escadas não).</summary>
    public bool Billable { get; init; } = true;

    /// <summary>Valor atual depreciado.</summary>
    public int CurrentValue { get; private set; }

    /// <summary>Piso de revenda em $Money (<see cref="EconomyPhysics.SalvageFloorPercent"/> do preço).</summary>
    public int SalvageFloor =>
        PurchasePrice * EconomyPhysics.SalvageFloorPercent / 100;

    /// <summary>
    /// Deprecia um dia: move <see cref="CurrentValue"/> rumo ao piso, sem
    /// ultrapassá-lo. <paramref name="percentPerDay"/> é uma fração do preço
    /// de compra (mesma cadência de passo do decaimento de relacionamento).
    /// </summary>
    public void DepreciateOneDay(int percentPerDay)
    {
        if (percentPerDay < 0)
            throw new ArgumentOutOfRangeException(
                nameof(percentPerDay), percentPerDay, "percentPerDay must be >= 0");

        int step = Math.Max(1, PurchasePrice * percentPerDay / 100);
        CurrentValue = Math.Max(SalvageFloor, CurrentValue - step);
    }
}
