using RelationshipSystem.Core;
using RelationshipSystem.Core.Interactions;

namespace EconomySystem.Core.Integration;

/// <summary>
/// Dá custo monetário a interações sociais SEM editar o núcleo de
/// relacionamentos: debita o caixa primeiro e só então delega ao
/// <see cref="InteractionResolver"/>. Se faltar dinheiro, a interação social não
/// acontece. Resolve o fato de o <c>GiveGift</c> do núcleo ser de graça.
/// </summary>
public sealed class PaidInteractionResolver
{
    private readonly InteractionResolver _social;

    /// <summary>Disparado quando o custo de uma interação é debitado.</summary>
    public event MoneyEventHandler? Spent;

    /// <summary>Disparado quando faltou dinheiro e a interação foi cancelada.</summary>
    public event Action<string, string>? Cancelled;

    public PaidInteractionResolver(InteractionResolver social)
    {
        _social = social ?? throw new ArgumentNullException(nameof(social));
    }

    /// <summary>
    /// Executa uma interação que custa <paramref name="cost"/> $Money. Debita
    /// o caixa; se não houver saldo, cancela (retorna false) sem tocar o
    /// relacionamento. Caso contrário, delega ao resolver social e retorna se a
    /// interação foi aceita.
    /// </summary>
    public bool PerformPaid(string householdId, HouseholdFunds funds, string from, string to,
        InteractionDefinition def, int cost)
    {
        if (string.IsNullOrWhiteSpace(householdId))
            throw new ArgumentException("householdId required", nameof(householdId));
        ArgumentNullException.ThrowIfNull(funds);
        ArgumentNullException.ThrowIfNull(def);
        if (cost < 0)
            throw new ArgumentOutOfRangeException(nameof(cost), cost, "cost must be >= 0");

        if (cost > 0)
        {
            var tx = new MoneyTransaction
            {
                Reason = $"{def.DisplayName} (de {from} para {to})",
                Amount = -cost,
                Kind = TransactionKind.Gift,
            };
            if (!funds.TryWithdraw(tx))
            {
                Cancelled?.Invoke(from, to);
                return false;
            }
            Spent?.Invoke(householdId, tx);
        }

        return _social.Perform(from, to, def);
    }

    /// <summary>Atalho: dar um presente pago (<see cref="SocialCosts.GiftCost"/>).</summary>
    public bool GiveGift(string householdId, HouseholdFunds funds, string from, string to) =>
        PerformPaid(householdId, funds, from, to, InteractionLibrary.GiveGift, SocialCosts.GiftCost);
}
