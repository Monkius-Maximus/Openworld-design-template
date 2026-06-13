namespace EconomySystem.Core.Market;

/// <summary>
/// Resultado de uma cotação de preço pelo pipeline do mercado:
/// <c>base($Money) × GlobalIndex × ProductIndex × UnitsPerMoney × CurrencyIndex</c>.
/// Arredondamento <c>AwayFromZero</c> só no passo final, nunca em fatores
/// intermediários (evita deriva de arredondamento composta).
/// </summary>
public readonly record struct PriceQuote(
    int BasePriceInMoney,
    decimal EffectiveMoneyPrice,
    decimal ConvertedPrice,
    int RoundedPrice,
    string CurrencySymbol)
{
    public override string ToString() => $"{CurrencySymbol}{RoundedPrice}";
}
