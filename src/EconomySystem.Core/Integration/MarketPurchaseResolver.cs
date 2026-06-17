using EconomySystem.Core.Market;

namespace EconomySystem.Core.Integration;

/// <summary>
/// Ponte mercado → caixa (v6): permite "gastar em moeda estrangeira" sem quebrar
/// o ledger mono-moeda do domicílio. O item é cotado numa moeda derivada (o que
/// o jogador vê) e o equivalente em $Money — já com o prêmio de inflação da
/// moeda — é debitado do caixa. Espelha o estilo dos outros resolvers de
/// Integration (aplica + emite evento).
/// </summary>
public sealed class MarketPurchaseResolver
{
    private readonly CurrencyMarket _market;

    public MarketPurchaseResolver(CurrencyMarket market) =>
        _market = market ?? throw new ArgumentNullException(nameof(market));

    /// <summary>Disparado a cada compra concluída (transação em $Money).</summary>
    public event MoneyEventHandler? Purchased;

    /// <summary>
    /// Tenta comprar um item cotado em <paramref name="currencyId"/>, debitando
    /// o equivalente em $Money do caixa. Retorna false (sem alterar o saldo) se
    /// faltar fundos. Item grátis (custo 0) "compra" sem mexer no caixa.
    /// </summary>
    public bool TryBuy(
        string householdId,
        HouseholdFunds funds,
        int basePriceInMoney,
        string currencyId,
        string? productId = null,
        string? label = null)
    {
        if (string.IsNullOrWhiteSpace(householdId))
            throw new ArgumentException("householdId required", nameof(householdId));
        ArgumentNullException.ThrowIfNull(funds);

        var quote = _market.Quote(basePriceInMoney, currencyId, productId);
        var currency = _market.Currencies.Get(currencyId);
        int costInMoney = (int)Math.Round(
            quote.RoundedPrice / currency.UnitsPerMoney, MidpointRounding.AwayFromZero);

        if (costInMoney <= 0)
            return true; // grátis: nada a debitar

        var tx = new MoneyTransaction
        {
            Reason = label ?? $"Compra cotada em {quote.CurrencySymbol}{quote.RoundedPrice}",
            Amount = -costInMoney,
            Kind = TransactionKind.Purchase,
            GameDay = _market.Calendar.CurrentDay,
            CurrencyId = currency.Id,
        };

        if (!funds.TryWithdraw(tx))
            return false;

        Purchased?.Invoke(householdId, tx);
        return true;
    }
}
