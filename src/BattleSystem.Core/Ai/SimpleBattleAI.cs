namespace BattleSystem.Core.Ai;

/// <summary>
/// IA heurística e determinística — o suficiente para a batalha ser FUNCIONAL
/// sem assets nem input de jogador (demo de console e protótipo Godot).
/// Regras, em ordem: curar-se em perigo → maior dano pagável → repor stamina
/// → primeira ação possível (Defender/Descansar têm custo zero, então sempre
/// existe escolha).
/// </summary>
public sealed class SimpleBattleAI
{
    /// <summary>Escolhe ação e alvo para o combatente da vez.</summary>
    public (BattleActionDefinition Action, Combatant Target) Choose(
        Battle battle, Combatant actor, IEnumerable<BattleActionDefinition> actions)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(actions);

        var pool = actions.ToList();

        // 1. Em perigo? Cura-se, se houver cura pagável.
        if (actor.HealthFraction <= BattleThresholds.LowHealthFraction)
        {
            var heal = pool.FirstOrDefault(d =>
                d.OnHit.Heal > 0f && Usable(d, actor, actor));
            if (heal is not null)
                return (heal, actor);
        }

        // 2. Ataca o inimigo mais ferido com o maior dano que a stamina paga.
        var target = battle.EnemiesOf(actor)
            .OrderBy(c => c.Health)
            .ThenBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (target is not null)
        {
            var attack = pool
                .Where(d => d.Target == ActionTarget.Enemy
                            && d.OnHit.Damage > 0f
                            && Usable(d, actor, target))
                .OrderByDescending(d => d.OnHit.Damage)
                .FirstOrDefault();
            if (attack is not null)
                return (attack, target);
        }

        // 3. Sem stamina para atacar: repõe.
        var rest = pool.FirstOrDefault(d =>
            d.OnHit.StaminaRestore > 0f && Usable(d, actor, actor));
        if (rest is not null)
            return (rest, actor);

        // 4. Último recurso: qualquer ação em si mesmo (Defender).
        var fallback = pool.FirstOrDefault(d =>
            d.Target == ActionTarget.Self && Usable(d, actor, actor));
        return fallback is not null
            ? (fallback, actor)
            : throw new InvalidOperationException($"Nenhuma ação disponível para '{actor.Id}'");
    }

    private static bool Usable(BattleActionDefinition def, Combatant actor, Combatant target) =>
        def.StaminaCost <= actor.Stamina && def.Available(actor, target);
}
