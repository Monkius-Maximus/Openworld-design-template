namespace BattleSystem.Core;

/// <summary>
/// Estado de runtime de um combatente: vida/stamina atuais (setters privados +
/// deltas com clamp, como <c>RelationshipValue</c>) e efeitos de status ativos.
/// Os atributos efetivos somam os bônus de status NÃO expirados (mesma regra
/// do <c>EffectiveDaily</c>, que ignora modificadores expirados).
/// </summary>
public sealed class Combatant
{
    public CombatantStats Stats { get; }

    public string Id => Stats.Id;

    public float Health { get; private set; }

    public float Stamina { get; private set; }

    /// <summary>Efeitos de status ativos (em guarda, envenenado, moral...).</summary>
    public List<StatusEffect> Statuses { get; } = new();

    public Combatant(CombatantStats stats)
    {
        Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        Health = stats.MaxHealth;
        Stamina = stats.MaxStamina;
    }

    public bool IsDefeated => Health <= 0f;

    /// <summary>Vida em fração [0..1]; "em perigo" abaixo de <see cref="BattleThresholds.LowHealthFraction"/>.</summary>
    public float HealthFraction => Health / Stats.MaxHealth;

    public float EffectiveAttack => Stats.Attack + ActiveStatuses().Sum(s => s.AttackBonus);

    public float EffectiveDefense => Stats.Defense + ActiveStatuses().Sum(s => s.DefenseBonus);

    /// <summary>Dano de status (veneno etc.) devido ao fim do turno do portador.</summary>
    public float PendingTurnDamage => ActiveStatuses().Sum(s => s.DamagePerTurn);

    /// <summary>Aplica dano. Retorna o dano efetivamente sofrido (vida não fica negativa).</summary>
    public float ApplyDamage(float amount)
    {
        if (amount < 0f)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "amount must be >= 0");

        float actual = Math.Min(amount, Health);
        Health -= actual;
        return actual;
    }

    /// <summary>Cura limitada ao máximo. Não ressuscita: derrotado permanece derrotado.</summary>
    public void Heal(float amount)
    {
        if (amount < 0f)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "amount must be >= 0");
        if (IsDefeated)
            return;

        Health = Math.Min(Health + amount, Stats.MaxHealth);
    }

    /// <summary>Tenta pagar o custo de stamina de uma ação. False se não alcança.</summary>
    public bool TrySpendStamina(float cost)
    {
        if (cost < 0f)
            throw new ArgumentOutOfRangeException(nameof(cost), cost, "cost must be >= 0");
        if (cost > Stamina)
            return false;

        Stamina -= cost;
        return true;
    }

    /// <summary>Recupera stamina limitada ao máximo (regen de turno, ação Descansar).</summary>
    public void RegenStamina(float amount)
    {
        if (amount < 0f)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "amount must be >= 0");

        Stamina = Math.Min(Stamina + amount, Stats.MaxStamina);
    }

    /// <summary>Adiciona um efeito de status (já clonado pelo chamador).</summary>
    public void AddStatus(StatusEffect status)
    {
        ArgumentNullException.ThrowIfNull(status);
        Statuses.Add(status);
    }

    /// <summary>Consome um turno de todos os efeitos; remove os expirados.</summary>
    public void TickStatuses()
    {
        Statuses.RemoveAll(s => s.TickTurn());
    }

    private IEnumerable<StatusEffect> ActiveStatuses() => Statuses.Where(s => !s.IsExpired);
}
