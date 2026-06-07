namespace EconomySystem.Core;

/// <summary>
/// Natureza de uma transação. Serve de rótulo para histórico e relatórios.
/// Os valores marcados "(fase posterior)" já existem para o esquema não
/// precisar mudar quando carreiras e a camada digital chegarem.
/// </summary>
public enum TransactionKind
{
    /// <summary>Conta do domicílio (luz, água, etc).</summary>
    Bill,

    /// <summary>Compra de um objeto (sai do caixa).</summary>
    Purchase,

    /// <summary>Venda avulsa (entra no caixa).</summary>
    Sale,

    /// <summary>Renda por habilidade (pintura, colheita, artesanato).</summary>
    Freelance,

    /// <summary>Reposição de estoque de um negócio (Open for Business).</summary>
    Restock,

    /// <summary>Salário pago a um funcionário do negócio.</summary>
    EmployeeWage,

    /// <summary>Custo de um presente dado numa interação social.</summary>
    Gift,

    /// <summary>Salário de carreira. (fase posterior)</summary>
    Wage,

    /// <summary>Assinatura digital recorrente. (fase posterior)</summary>
    Subscription,

    /// <summary>Resultado de chance card no trabalho. (fase posterior)</summary>
    ChanceCard,
}

/// <summary>
/// Lançamento nomeado e justificado no caixa — o análogo econômico de um
/// <c>RelationshipModifier</c> (estilo Crusader Kings 3): toda movimentação de
/// Simoleons carrega um motivo legível. É um <c>record</c> imutável; o resolver
/// clona o template antes de aplicar, como faz o <c>InteractionResolver</c>.
/// </summary>
public sealed class MoneyTransaction
{
    private readonly string _reason = string.Empty;

    /// <summary>Motivo legível (ex.: "Venda de pintura"). Obrigatório.</summary>
    public required string Reason
    {
        get => _reason;
        init => _reason = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Transaction reason required", nameof(Reason))
            : value;
    }

    /// <summary>Valor em Simoleons (§). Positivo = entrada, negativo = saída.</summary>
    public required int Amount { get; init; }

    /// <summary>Natureza da transação.</summary>
    public required TransactionKind Kind { get; init; }

    /// <summary>Dia do jogo em que ocorreu (bookkeeping opcional).</summary>
    public int? GameDay { get; init; }

    /// <summary>Cria uma cópia idêntica — usada ao aplicar um template.</summary>
    public MoneyTransaction Clone() => new()
    {
        Reason = Reason,
        Amount = Amount,
        Kind = Kind,
        GameDay = GameDay,
    };
}
