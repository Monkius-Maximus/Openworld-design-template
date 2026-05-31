namespace RelationshipSystem.Core;

/// <summary>
/// Tabela de compatibilidade de signos, inspirada no The Sims 2.
/// A compatibilidade é derivada dos quatro elementos clássicos
/// (Fogo, Terra, Ar, Água):
/// <list type="bullet">
///   <item>Mesmo signo: 0 (nenhum signo favorece a si mesmo).</item>
///   <item>Mesmo elemento (signos distintos): +2 estrelas.</item>
///   <item>Elementos complementares (Fogo↔Ar, Terra↔Água): +1.</item>
///   <item>Elementos em tensão (Fogo↔Água, Terra↔Ar): -1.</item>
///   <item>Demais combinações: 0.</item>
/// </list>
/// O resultado em estrelas é escalado para o intervalo
/// [-<see cref="AttractionWeights.ZodiacCompatibilityMax"/>,
///  +<see cref="AttractionWeights.ZodiacCompatibilityMax"/>].
/// </summary>
public static class ZodiacCompatibilityTable
{
    private enum Element { Fire, Earth, Air, Water }

    private const int StarScale = AttractionWeights.ZodiacCompatibilityMax / 2; // 2 estrelas => max

    private static Element ElementOf(Zodiac z) => z switch
    {
        Zodiac.Aries or Zodiac.Leo or Zodiac.Sagittarius => Element.Fire,
        Zodiac.Taurus or Zodiac.Virgo or Zodiac.Capricorn => Element.Earth,
        Zodiac.Gemini or Zodiac.Libra or Zodiac.Aquarius => Element.Air,
        Zodiac.Cancer or Zodiac.Scorpio or Zodiac.Pisces => Element.Water,
        _ => throw new ArgumentOutOfRangeException(nameof(z), z, "Unknown zodiac sign")
    };

    /// <summary>
    /// Modificador de compatibilidade de A em direção a B.
    /// É simétrico por elementos, mas mesmo signo retorna 0.
    /// </summary>
    public static int Get(Zodiac a, Zodiac b)
    {
        if (a == b)
            return 0;

        int stars = StarsForElements(ElementOf(a), ElementOf(b));
        return stars * StarScale;
    }

    private static int StarsForElements(Element x, Element y)
    {
        if (x == y)
            return 2; // mesmo elemento, signos diferentes

        return (x, y) switch
        {
            (Element.Fire, Element.Air) or (Element.Air, Element.Fire) => 1,
            (Element.Earth, Element.Water) or (Element.Water, Element.Earth) => 1,
            (Element.Fire, Element.Water) or (Element.Water, Element.Fire) => -1,
            (Element.Earth, Element.Air) or (Element.Air, Element.Earth) => -1,
            _ => 0
        };
    }
}
