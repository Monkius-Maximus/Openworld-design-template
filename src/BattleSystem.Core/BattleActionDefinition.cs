namespace BattleSystem.Core;

/// <summary>A quem a ação se dirige.</summary>
public enum ActionTarget
{
    /// <summary>Um inimigo vivo.</summary>
    Enemy,

    /// <summary>Um aliado vivo (incluindo o próprio ator).</summary>
    Ally,

    /// <summary>Somente o próprio ator.</summary>
    Self,
}

/// <summary>
/// Efeito de uma ação de combate (espelha <c>InteractionEffect</c>).
/// </summary>
/// <param name="Damage">Dano-base; o dano final soma ataque efetivo e subtrai defesa efetiva (mínimo <see cref="CombatPhysics.MinDamage"/>).</param>
/// <param name="Heal">Cura aplicada ao alvo.</param>
/// <param name="StaminaRestore">Stamina devolvida ao ATOR (ação Descansar).</param>
/// <param name="TargetStatus">Template de status aplicado ao alvo (clonado ao aplicar).</param>
/// <param name="SelfStatus">Template de status aplicado ao ator (clonado ao aplicar).</param>
public sealed record BattleActionEffect(
    float Damage = 0f,
    float Heal = 0f,
    float StaminaRestore = 0f,
    StatusEffect? TargetStatus = null,
    StatusEffect? SelfStatus = null
);

/// <summary>
/// Definição declarativa de uma ação de combate (espelha
/// <c>InteractionDefinition</c>: Available como pré-condição fail-fast,
/// HitChance como regra de sucesso, efeitos separados para acerto/erro).
/// É o substituto agnóstico de engine do que na Unreal viveria num
/// GameplayAbility (GAS): dados + predicados puros, sem nós nem assets.
/// </summary>
public sealed class BattleActionDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>Custo de stamina pago ao executar (falha rápido se não alcança).</summary>
    public required float StaminaCost { get; init; }

    public required ActionTarget Target { get; init; }

    /// <summary>
    /// Pré-condição: a ação pode ser usada por <c>actor</c> contra <c>target</c>?
    /// Exemplo: <c>(actor, target) => !target.IsDefeated</c>.
    /// </summary>
    public required Func<Combatant, Combatant, bool> Available { get; init; }

    /// <summary>
    /// Chance de sucesso [0..1] em função de ator e alvo (pode usar velocidade,
    /// status etc.). O resolver sorteia contra ela.
    /// </summary>
    public required Func<Combatant, Combatant, double> HitChance { get; init; }

    /// <summary>Efeitos se acerta.</summary>
    public required BattleActionEffect OnHit { get; init; }

    /// <summary>Efeitos se erra (ex.: golpe pesado deixa desequilibrado).</summary>
    public required BattleActionEffect OnMiss { get; init; }
}
