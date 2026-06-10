namespace BattleSystem.Core;

public delegate void BattleActionHandler(Combatant actor, Combatant target, BattleActionDefinition def, bool hit);

public delegate void DamageDealtHandler(Combatant source, Combatant target, float amount);

public delegate void CombatantHandler(Combatant combatant);

public delegate void BattleEndedHandler(Battle battle, BattleTeam winner);

/// <summary>
/// Executa ações de combate, aplica efeitos e emite eventos — o análogo do
/// <c>InteractionResolver</c>. Eventos C# puros: no Godot, o adaptador os
/// assina e os re-emite como <c>[Signal]</c> (substituindo os delegates/
/// dispatchers da Unreal).
/// </summary>
public sealed class BattleResolver
{
    private readonly Battle _battle;
    private readonly Random _rng;

    public event BattleActionHandler? ActionPerformed;
    public event DamageDealtHandler? DamageDealt;
    public event CombatantHandler? CombatantDefeated;
    public event BattleEndedHandler? BattleEnded;

    /// <summary>
    /// <paramref name="rng"/> é injetável para testes determinísticos
    /// (seed fixa); por padrão usa <see cref="Random.Shared"/>.
    /// </summary>
    public BattleResolver(Battle battle, Random? rng = null)
    {
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
        _rng = rng ?? Random.Shared;
    }

    public Battle Battle => _battle;

    /// <summary>
    /// Executa a ação do combatente da vez. Falha rápido se a batalha acabou,
    /// se não é o turno do ator, se o alvo é inválido para o tipo da ação, se a
    /// pré-condição <c>Available</c> reprova ou se a stamina não alcança.
    /// Retorna true se a ação acertou. <paramref name="targetId"/> é ignorado
    /// para ações <see cref="ActionTarget.Self"/>.
    /// </summary>
    public bool Perform(string actorId, BattleActionDefinition def, string? targetId = null)
    {
        ArgumentNullException.ThrowIfNull(def);

        // FALHA RÁPIDO
        if (_battle.IsOver)
            throw new InvalidOperationException($"Batalha '{_battle.Id}' já terminou");

        var actor = _battle.Find(actorId);
        if (actor != _battle.Active)
            throw new InvalidOperationException($"Não é o turno de '{actorId}'");

        var target = ResolveTarget(actor, def, targetId);

        if (!def.Available(actor, target))
            throw new InvalidOperationException($"Ação '{def.Id}' indisponível: {actor.Id} → {target.Id}");

        if (!actor.TrySpendStamina(def.StaminaCost))
            throw new InvalidOperationException($"Stamina insuficiente para '{def.Id}' ({actor.Id})");

        double chance = Math.Clamp(def.HitChance(actor, target), 0.0, 1.0);
        bool hit = _rng.NextDouble() < chance;
        var effect = hit ? def.OnHit : def.OnMiss;

        // Anuncia a ação antes das consequências (dano, derrota, fim), para que
        // assinantes (log do console, sinais do Godot) vejam a ordem natural.
        ActionPerformed?.Invoke(actor, target, def, hit);

        // Dano: base + ataque efetivo - defesa efetiva, nunca abaixo do mínimo.
        if (effect.Damage > 0f)
        {
            float raw = effect.Damage + actor.EffectiveAttack - target.EffectiveDefense;
            float dealt = target.ApplyDamage(Math.Max(CombatPhysics.MinDamage, raw));
            DamageDealt?.Invoke(actor, target, dealt);

            if (target.IsDefeated)
                CombatantDefeated?.Invoke(target);
        }

        if (effect.Heal > 0f)
            target.Heal(effect.Heal);

        if (effect.StaminaRestore > 0f)
            actor.RegenStamina(effect.StaminaRestore);

        // Status são templates: clona ao aplicar para não compartilhar
        // RemainingTurns (mutável) entre combatentes.
        if (effect.TargetStatus is { } targetTemplate)
            target.AddStatus(targetTemplate.Clone());

        if (effect.SelfStatus is { } selfTemplate)
            actor.AddStatus(selfTemplate.Clone());

        AnnounceEndIfOver();

        return hit;
    }

    /// <summary>Emite <see cref="BattleEnded"/> uma única vez (resolver ou turno, quem vir primeiro).</summary>
    internal void AnnounceEndIfOver()
    {
        if (_battle.IsOver && !_battle.EndAnnounced && _battle.Winner is { } winner)
        {
            _battle.EndAnnounced = true;
            BattleEnded?.Invoke(_battle, winner);
        }
    }

    private Combatant ResolveTarget(Combatant actor, BattleActionDefinition def, string? targetId)
    {
        if (def.Target == ActionTarget.Self)
            return actor;

        if (string.IsNullOrWhiteSpace(targetId))
            throw new ArgumentException($"Ação '{def.Id}' exige um alvo", nameof(targetId));

        var target = _battle.Find(targetId);

        if (target.IsDefeated)
            throw new InvalidOperationException($"Alvo '{target.Id}' já foi derrotado");

        bool sameTeam = _battle.TeamOf(actor) == _battle.TeamOf(target);
        return def.Target switch
        {
            ActionTarget.Enemy when sameTeam =>
                throw new InvalidOperationException($"Ação '{def.Id}' exige um inimigo; '{target.Id}' é aliado"),
            ActionTarget.Ally when !sameTeam =>
                throw new InvalidOperationException($"Ação '{def.Id}' exige um aliado; '{target.Id}' é inimigo"),
            _ => target,
        };
    }
}
