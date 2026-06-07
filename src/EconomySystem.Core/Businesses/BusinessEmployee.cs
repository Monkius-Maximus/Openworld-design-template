namespace EconomySystem.Core.Businesses;

/// <summary>Papel de um funcionário num negócio (Open for Business).</summary>
public enum EmployeeRole
{
    /// <summary>Repõe os itens vendidos nas prateleiras.</summary>
    Restocker,

    /// <summary>Opera o caixa.</summary>
    Cashier,

    /// <summary>Aborda e convence clientes.</summary>
    Sales,
}

/// <summary>
/// Funcionário de um negócio. O salário diário deriva do papel
/// (<see cref="BusinessRules"/>). Pago pelo tick (folha — fase posterior).
/// </summary>
public sealed class BusinessEmployee
{
    private readonly string _characterId = string.Empty;

    public required string CharacterId
    {
        get => _characterId;
        init => _characterId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("CharacterId required", nameof(CharacterId))
            : value;
    }

    public required EmployeeRole Role { get; init; }

    /// <summary>Talent badge 0..5 (estilo TS2: define velocidade/eficiência).</summary>
    public int BadgeLevel { get; set; }

    /// <summary>Salário diário, conforme o papel.</summary>
    public int DailyWage => Role switch
    {
        EmployeeRole.Restocker => BusinessRules.RestockerDailyWage,
        EmployeeRole.Cashier => BusinessRules.CashierDailyWage,
        EmployeeRole.Sales => BusinessRules.SalesDailyWage,
        _ => 0,
    };
}
