namespace EconomySystem.Core;

/// <summary>
/// Caixa compartilhado de um domicílio (estilo The Sims 2: um único bolso por
/// família, em $Money inteiros). Espelha o <c>RelationshipValue</c>: estado
/// mutável com setter privado e métodos de delta. Por padrão NÃO há dívida —
/// <see cref="TryWithdraw"/> falha se faltar saldo (é o gancho que o
/// <c>BillsSystem</c> usa para acionar o repo-man).
/// </summary>
public sealed class HouseholdFunds
{
    private readonly List<MoneyTransaction> _history = new();

    public HouseholdFunds(int startingBalance = 0)
    {
        if (startingBalance < 0)
            throw new ArgumentOutOfRangeException(
                nameof(startingBalance), startingBalance, "startingBalance must be >= 0");
        Balance = startingBalance;
    }

    /// <summary>Saldo atual em $Money (nunca negativo).</summary>
    public int Balance { get; private set; }

    /// <summary>Últimas transações (mais recentes ao fim), limitado.</summary>
    public IReadOnlyList<MoneyTransaction> History => _history;

    /// <summary>Credita uma entrada. O valor da transação deve ser &gt; 0.</summary>
    public void Deposit(MoneyTransaction tx)
    {
        ArgumentNullException.ThrowIfNull(tx);
        if (tx.Amount <= 0)
            throw new ArgumentException("Deposit amount must be > 0", nameof(tx));

        Balance += tx.Amount;
        Record(tx);
    }

    /// <summary>
    /// Tenta debitar uma saída. <paramref name="tx"/> deve ter <c>Amount</c>
    /// negativo. Retorna false (sem alterar o saldo) se não houver fundos.
    /// </summary>
    public bool TryWithdraw(MoneyTransaction tx)
    {
        ArgumentNullException.ThrowIfNull(tx);
        if (tx.Amount >= 0)
            throw new ArgumentException("Withdrawal amount must be < 0", nameof(tx));

        if (Balance + tx.Amount < 0)
            return false;

        Balance += tx.Amount;
        Record(tx);
        return true;
    }

    /// <summary>Ajuste direto (debug/reset), no espírito de <c>RelationshipValue.Reset</c>.</summary>
    public void ForceAdjust(int delta)
    {
        Balance = Math.Max(0, Balance + delta);
    }

    private void Record(MoneyTransaction tx)
    {
        _history.Add(tx);
        if (_history.Count > EconomyThresholds.MaxHistoryEntries)
            _history.RemoveAt(0);
    }
}
