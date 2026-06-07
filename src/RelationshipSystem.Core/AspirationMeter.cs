namespace RelationshipSystem.Core;

/// <summary>
/// Barra de aspiração de um personagem (The Sims 2): sobe ao cumprir Wants,
/// desce ao realizar Fears. Limitada a [-100, +100] (do vermelho ao platina).
/// </summary>
public sealed class AspirationMeter
{
    public const float Min = -100f;
    public const float Max = 100f;

    public float Score { get; private set; }

    /// <summary>Aplica um delta (positivo ou negativo), com clamp aos limites.</summary>
    public void Apply(int delta) => Score = Math.Clamp(Score + delta, Min, Max);
}
