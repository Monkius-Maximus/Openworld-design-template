namespace RelationshipSystem.Core;

/// <summary>
/// Sistema de passagem de tempo para relacionamentos.
/// </summary>
public sealed class RelationshipDecaySystem
{
    /// <summary>
    /// Chamado 1x por dia do jogo: decai daily por falta de contato e
    /// envelhece os modificadores em 24 horas.
    /// </summary>
    public void DailyTick(RelationshipMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        foreach (var rel in matrix.All)
        {
            rel.Value.DecayDailyTowardZero(RelationshipPhysics.DailyDecayPerDay);
            rel.DecayModifiers(24f);   // 24 horas
            rel.DecaySentiments(24f);  // curto prazo envelhece; longo prazo é imune
        }
    }

    /// <summary>
    /// Chamado 3x por dia: normaliza lifetime em direção ao daily.
    /// </summary>
    public void NormalizationTick(RelationshipMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        foreach (var rel in matrix.All)
        {
            rel.Value.NormalizeLifetimeTowardDaily(RelationshipPhysics.LifetimeNormalizationPerTick);
        }
    }
}
