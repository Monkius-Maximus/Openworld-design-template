namespace EconomySystem.Core;

/// <summary>
/// Unidade econômica do jogo: um domicílio com caixa compartilhado, inventário
/// de bens e os personagens que o habitam. É a "entrada" sobre a qual o
/// <c>EconomyTickSystem</c> itera, equivalente a uma entrada do
/// <c>RelationshipMatrix</c> no núcleo de relacionamentos.
/// </summary>
public sealed class Household
{
    private readonly string _id = string.Empty;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Household id required", nameof(Id))
            : value;
    }

    /// <summary>Caixa compartilhado.</summary>
    public HouseholdFunds Funds { get; init; } = new();

    /// <summary>Bens do domicílio.</summary>
    public HouseholdInventory Inventory { get; } = new();

    /// <summary>Ids dos personagens que moram aqui (chaves do RelationshipMatrix).</summary>
    public List<string> MemberIds { get; } = new();

    /// <summary>Empregos dos moradores, por id de personagem (v2).</summary>
    public Dictionary<string, CareerState> Careers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Assinaturas digitais recorrentes do domicílio (v2).</summary>
    public List<Subscription> Subscriptions { get; } = new();

    /// <summary>Número de crianças — desconta 10% por filho na conta (TS2).</summary>
    public int ChildCount { get; set; }

    /// <summary>Dias restantes de tolerância antes do repo-man (0 = em dia).</summary>
    public int RepoGraceDaysRemaining { get; set; }

    /// <summary>Patrimônio líquido = caixa + valor dos bens (TS2).</summary>
    public int NetWorth => Funds.Balance + Inventory.TotalObjectValue;
}
