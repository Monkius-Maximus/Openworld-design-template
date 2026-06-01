namespace RelationshipSystem.Core;

/// <summary>
/// Tabela de compatibilidade de signos no estilo The Sims 2: uma matriz
/// literal 12×12 (uma célula por par ordenado de signos), consultada por
/// índice do <see cref="Zodiac"/>.
/// <para>
/// Os valores proprietários exatos do TS2 não são canônicos publicamente; o
/// efeito do jogo reduz-se à compatibilidade astrológica clássica, derivada do
/// aspecto angular entre os signos (a distância no círculo de 12 posições).
/// As células abaixo codificam esses aspectos, escalados para o intervalo
/// [-<see cref="AttractionWeights.ZodiacCompatibilityMax"/>,
///  +<see cref="AttractionWeights.ZodiacCompatibilityMax"/>]:
/// </para>
/// <list type="table">
///   <listheader><term>Aspecto (distância)</term><description>Valor</description></listheader>
///   <item><term>Conjunção — mesmo signo (0)</term><description>0 — nenhum signo favorece a si mesmo</description></item>
///   <item><term>Semisextil (1)</term><description>-10 — desajeitado</description></item>
///   <item><term>Sextil (2)</term><description>+15 — harmonioso (Fogo↔Ar, Terra↔Água)</description></item>
///   <item><term>Quadratura (3)</term><description>-15 — tensão (Fogo↔Água, Terra↔Ar)</description></item>
///   <item><term>Trígono (4)</term><description>+30 — mesmo elemento, máximo</description></item>
///   <item><term>Quincúncio (5)</term><description>-10 — desajeitado</description></item>
///   <item><term>Oposição (6)</term><description>+20 — atração complementar</description></item>
/// </list>
/// A grade é simétrica (<c>Get(a, b) == Get(b, a)</c>); a assimetria da atração
/// total vem dos turn-ons/turn-off, não do zodíaco.
/// </summary>
public static class ZodiacCompatibilityTable
{
    // Ordem das linhas/colunas = ordem do enum Zodiac:
    // 0 Aries, 1 Taurus, 2 Gemini, 3 Cancer, 4 Leo, 5 Virgo,
    // 6 Libra, 7 Scorpio, 8 Sagittarius, 9 Capricorn, 10 Aquarius, 11 Pisces.
    private static readonly int[,] Table =
    {
        //          Ari  Tau  Gem  Can  Leo  Vir  Lib  Sco  Sag  Cap  Aqu  Pis
        /* Ari */ {   0, -10,  15, -15,  30, -10,  20, -10,  30, -15,  15, -10 },
        /* Tau */ { -10,   0, -10,  15, -15,  30, -10,  20, -10,  30, -15,  15 },
        /* Gem */ {  15, -10,   0, -10,  15, -15,  30, -10,  20, -10,  30, -15 },
        /* Can */ { -15,  15, -10,   0, -10,  15, -15,  30, -10,  20, -10,  30 },
        /* Leo */ {  30, -15,  15, -10,   0, -10,  15, -15,  30, -10,  20, -10 },
        /* Vir */ { -10,  30, -15,  15, -10,   0, -10,  15, -15,  30, -10,  20 },
        /* Lib */ {  20, -10,  30, -15,  15, -10,   0, -10,  15, -15,  30, -10 },
        /* Sco */ { -10,  20, -10,  30, -15,  15, -10,   0, -10,  15, -15,  30 },
        /* Sag */ {  30, -10,  20, -10,  30, -15,  15, -10,   0, -10,  15, -15 },
        /* Cap */ { -15,  30, -10,  20, -10,  30, -15,  15, -10,   0, -10,  15 },
        /* Aqu */ {  15, -15,  30, -10,  20, -10,  30, -15,  15, -10,   0, -10 },
        /* Pis */ { -10,  15, -15,  30, -10,  20, -10,  30, -15,  15, -10,   0 },
    };

    /// <summary>
    /// Modificador de compatibilidade do signo <paramref name="a"/> em relação a
    /// <paramref name="b"/>, no intervalo [-30, +30]. Mesmo signo retorna 0.
    /// </summary>
    public static int Get(Zodiac a, Zodiac b) => Table[(int)a, (int)b];
}
