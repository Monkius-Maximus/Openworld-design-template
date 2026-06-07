namespace EconomySystem.Core;

/// <summary>
/// Executa uma atividade de renda sobre o caixa de um domicílio. Espelha o
/// <c>InteractionResolver</c>: falha rápido na pré-condição, aplica o resultado
/// clonando o template de transação, e emite eventos.
/// </summary>
public sealed class IncomeResolver
{
    // Random compartilhado — evita o anti-padrão de instanciar Random em loop.
    private static readonly Random Rng = Random.Shared;

    /// <summary>Disparado quando uma atividade credita dinheiro.</summary>
    public event MoneyEventHandler? Earned;

    /// <summary>
    /// Executa a atividade para um personagem com dada habilidade. Falha rápido
    /// se a atividade está indisponível. Retorna true se foi bem-sucedida.
    /// </summary>
    public bool Perform(string householdId, HouseholdFunds funds, IncomeActivityDefinition def, int skillLevel)
    {
        if (string.IsNullOrWhiteSpace(householdId))
            throw new ArgumentException("householdId required", nameof(householdId));
        ArgumentNullException.ThrowIfNull(funds);
        ArgumentNullException.ThrowIfNull(def);

        var ctx = new IncomeContext(skillLevel, Rng.NextDouble());

        // FALHA RÁPIDO
        if (!def.Available(ctx))
        {
            throw new InvalidOperationException(
                $"Atividade '{def.Id}' indisponível (habilidade {skillLevel}).");
        }

        bool success = def.Succeeds(ctx);
        var outcome = success ? def.OnSuccess : def.OnFailure;

        // Clona o template para não compartilhar a instância entre domicílios.
        var tx = outcome.Template.Clone();
        if (tx.Amount > 0)
        {
            funds.Deposit(tx);
            Earned?.Invoke(householdId, tx);
        }

        return success;
    }
}
