namespace BattleSystem.Core;

/// <summary>
/// Efeito de status nomeado e temporário sobre um combatente — o análogo de
/// combate do <c>RelationshipModifier</c> (CK3), mas medido em TURNOS em vez
/// de horas. Exemplos: "Em guarda" (+defesa, 1 turno), "Envenenado"
/// (dano por turno, 3 turnos).
/// </summary>
public sealed class StatusEffect
{
    private readonly string _name = string.Empty;

    public required string Name
    {
        get => _name;
        init => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Status name required", nameof(Name))
            : value;
    }

    public float AttackBonus { get; init; }

    public float DefenseBonus { get; init; }

    /// <summary>Dano sofrido ao fim de cada turno do portador (veneno, sangramento).</summary>
    public float DamagePerTurn { get; init; }

    /// <summary>Turnos restantes (do próprio portador).</summary>
    public int RemainingTurns { get; set; }

    public bool IsExpired => RemainingTurns <= 0;

    /// <summary>Consome um turno. Retorna true se expirou neste tick.</summary>
    public bool TickTurn()
    {
        RemainingTurns--;
        return IsExpired;
    }

    /// <summary>
    /// Clona o efeito. As definições de ação carregam um *template*; quem aplica
    /// clona para não compartilhar <see cref="RemainingTurns"/> (mutável) entre
    /// combatentes — mesma decisão dos modificadores de relacionamento.
    /// </summary>
    public StatusEffect Clone() => new()
    {
        Name = Name,
        AttackBonus = AttackBonus,
        DefenseBonus = DefenseBonus,
        DamagePerTurn = DamagePerTurn,
        RemainingTurns = RemainingTurns,
    };
}
