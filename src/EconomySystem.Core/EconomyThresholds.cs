namespace EconomySystem.Core;

/// <summary>
/// Limiares e parâmetros gerais da economia (defaults estilo The Sims 2).
/// Tunáveis por projeto. Espelha <c>RelationshipThresholds</c>.
/// </summary>
public static class EconomyThresholds
{
    /// <summary>Caixa inicial de um domicílio novo (TS2: §20.000).</summary>
    public const int StartingFunds = 20_000;

    /// <summary>Dias de tolerância para pagar antes do repo-man aparecer.</summary>
    public const int RepoManGraceDays = 2;

    /// <summary>Máximo de transações guardadas no histórico do caixa.</summary>
    public const int MaxHistoryEntries = 256;

    /// <summary>
    /// Mínimo de vendas acumuladas (em §) para ALCANÇAR cada estrela de
    /// fidelidade, de 1 a 5 estrelas (Open for Business). Gasto 0 = 0 estrelas.
    /// </summary>
    public static readonly IReadOnlyList<int> LoyaltyStarThresholds =
        new[] { 500, 2_000, 5_000, 10_000, 20_000 };
}

/// <summary>
/// "Física" econômica: taxas e cadências. Espelha <c>RelationshipPhysics</c>.
/// </summary>
public static class EconomyPhysics
{
    /// <summary>Conta ≈ valor faturável dos objetos × esta taxa (TS2 ≈ 3%).</summary>
    public const int BillRatePercent = 3;

    /// <summary>Quanto um objeto deprecia por dia, em % do preço de compra.</summary>
    public const int DepreciationPercentPerDay = 2;

    /// <summary>Piso de revenda de um objeto, em % do preço de compra.</summary>
    public const int SalvageFloorPercent = 40;

    /// <summary>Desconto na conta por cada criança no domicílio (TS2: 10%).</summary>
    public const int ChildBillDiscountPercent = 10;

    /// <summary>Dias da semana em que as contas chegam (TS2: terça e quinta).</summary>
    public static readonly IReadOnlyList<DayOfWeek> BillDeliveryDays =
        new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday };
}

/// <summary>
/// Regras do negócio próprio (Open for Business). Em grande parte usadas pela
/// camada <c>Business/</c> (lógica completa em fase posterior).
/// </summary>
public static class BusinessRules
{
    /// <summary>Markup padrão de venda sobre o custo do estoque, em %.</summary>
    public const int DefaultMarkupPercent = 20;

    /// <summary>Markup máximo antes de afugentar clientes, em %.</summary>
    public const int MaxMarkupPercent = 100;

    /// <summary>Salário diário de um funcionário, por papel.</summary>
    public const int RestockerDailyWage = 80;
    public const int CashierDailyWage = 90;
    public const int SalesDailyWage = 110;

    /// <summary>Bônus de "Bom atendimento" devolvido ao relacionamento numa venda satisfatória.</summary>
    public const float GoodServiceModifierValue = 8f;
    public const float GoodServiceModifierHours = 48f;
}
