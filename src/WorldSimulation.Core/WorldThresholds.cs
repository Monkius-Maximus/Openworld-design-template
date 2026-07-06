namespace WorldSimulation.Core;

/// <summary>
/// Constantes da passagem de tempo do mundo (mesmo papel dos demais
/// *Thresholds). O ritmo real↔jogo fica na camada de engine; aqui só o que é
/// regra de simulação.
/// </summary>
public static class WorldThresholds
{
    /// <summary>Horas do dia em que o lifetime normaliza rumo ao daily (3×/dia, TS2).</summary>
    public static readonly IReadOnlyList<int> NormalizationHours = new[] { 8, 14, 20 };

    /// <summary>Hora em que um mundo novo começa (o dia 0 já está em andamento).</summary>
    public const int DefaultStartHour = 8;

    /// <summary>Caixa inicial do domicílio do jogador no mundo de demonstração.</summary>
    public const int DemoStartingFunds = 1200;

    /// <summary>Caixa inicial do domicílio vizinho no mundo de demonstração.</summary>
    public const int DemoNeighborFunds = 800;
}
