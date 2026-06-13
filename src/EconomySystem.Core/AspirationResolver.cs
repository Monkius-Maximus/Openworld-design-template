namespace EconomySystem.Core;

/// <summary>
/// Resgata recompensas de aspiração: debita pontos da carteira e emite evento.
/// Espelha o estilo dos demais resolvers (aplica + evento). A moeda "soft" é
/// totalmente separada do caixa de $Money.
/// </summary>
public sealed class AspirationResolver
{
    /// <summary>Disparado quando uma recompensa é resgatada.</summary>
    public event AspirationRedeemedHandler? Redeemed;

    /// <summary>
    /// Tenta resgatar a recompensa para o personagem. Retorna false (sem gastar)
    /// se faltam pontos.
    /// </summary>
    public bool TryRedeem(string characterId, AspirationWallet wallet, AspirationReward reward)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            throw new ArgumentException("characterId required", nameof(characterId));
        ArgumentNullException.ThrowIfNull(wallet);
        ArgumentNullException.ThrowIfNull(reward);

        if (!wallet.TrySpend(reward.PointCost))
            return false;

        Redeemed?.Invoke(characterId, reward);
        return true;
    }
}
