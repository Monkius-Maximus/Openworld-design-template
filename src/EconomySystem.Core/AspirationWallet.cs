namespace EconomySystem.Core;

/// <summary>
/// Carteira de PONTOS DE ASPIRAÇÃO de UM personagem — a moeda "soft" paralela do
/// The Sims 2, separada dos Simoleons (assim como a atração é um eixo separado do
/// relacionamento). Ganha-se preenchendo a barra de aspiração; gasta-se em
/// objetos de recompensa. Espelha <c>HouseholdFunds</c> (setter privado + deltas).
/// </summary>
public sealed class AspirationWallet
{
    public AspirationWallet(int startingPoints = 0)
    {
        if (startingPoints < 0)
            throw new ArgumentOutOfRangeException(
                nameof(startingPoints), startingPoints, "startingPoints must be >= 0");
        Points = startingPoints;
    }

    /// <summary>Pontos de aspiração acumulados (nunca negativo).</summary>
    public int Points { get; private set; }

    /// <summary>Acumula pontos (barra de aspiração preenchida).</summary>
    public void Earn(int points)
    {
        if (points <= 0)
            throw new ArgumentOutOfRangeException(nameof(points), points, "points must be > 0");
        Points += points;
    }

    /// <summary>Gasta pontos. Retorna false (sem alterar) se não houver o suficiente.</summary>
    public bool TrySpend(int points)
    {
        if (points <= 0)
            throw new ArgumentOutOfRangeException(nameof(points), points, "points must be > 0");
        if (Points < points)
            return false;
        Points -= points;
        return true;
    }
}
