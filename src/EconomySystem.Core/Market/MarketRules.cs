namespace EconomySystem.Core.Market;

/// <summary>
/// Regras e parâmetros do mercado financeiro (v4). Espelha
/// <c>EconomyPhysics</c>: constantes tunáveis por projeto. O mercado é uma
/// camada OPT-IN sobre o núcleo "simplicidade amplificada" — o caixa dos
/// domicílios continua mono-moeda, em $Money inteiros.
/// </summary>
public static class MarketRules
{
    /// <summary>Id canônico da moeda base do motor.</summary>
    public const string BaseCurrencyId = "Money";

    /// <summary>Nome de exibição da moeda base.</summary>
    public const string BaseCurrencyName = "$Money";

    /// <summary>Símbolo curto da moeda base.</summary>
    public const string BaseCurrencySymbol = "$M";

    /// <summary>
    /// Dias num ano de simulação. 364 = 52 semanas × 7: divisível por 7, então
    /// o contador de anos nunca desalinha do ciclo de contas (terça/quinta).
    /// </summary>
    public const int DaysPerYear = 364;

    /// <summary>
    /// Preço de catálogo do produto-exemplo ("carro") usado no preview da UI
    /// de moedas, em $Money.
    /// </summary>
    public const int SamplePreviewPriceMoney = 10_000;

    /// <summary>Menor taxa de conversão aceita (1 $Money = N unidades).</summary>
    public const decimal MinUnitsPerMoney = 0.0001m;

    /// <summary>Inflação anual projetada máxima aceita, em %.</summary>
    public const decimal MaxAnnualInflationPercent = 1_000m;

    /// <summary>Inflação anual projetada mínima aceita (deflação), em %.</summary>
    public const decimal MinAnnualInflationPercent = -50m;

    /// <summary>
    /// Duração padrão de um evento econômico (v5), em dias. Meio ano de
    /// simulação — os presets de <c>EconomicEventLibrary</c> derivam daqui.
    /// </summary>
    public const int DefaultEventDurationDays = DaysPerYear / 2;
}
