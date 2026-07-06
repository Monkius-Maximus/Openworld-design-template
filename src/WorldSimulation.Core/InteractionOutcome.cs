namespace WorldSimulation.Core;

/// <summary>
/// Resultado de uma tentativa de interação social vinda da UI. Interação
/// indisponível NÃO é exceção aqui (a UI é exploratória por natureza);
/// exceções ficam para violações de contrato do núcleo.
/// </summary>
public readonly record struct InteractionOutcome(bool Performed, bool Accepted, string Message)
{
    public static InteractionOutcome Unavailable(string message) => new(false, false, message);
}
