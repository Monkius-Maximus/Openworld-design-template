namespace BattleSystem.Core;

public delegate void TurnEndedHandler(Battle battle, Combatant combatant);

/// <summary>
/// Passagem de tempo da batalha (espelha <c>RelationshipDecaySystem</c> /
/// <c>EconomyTickSystem</c>): ao fim do turno do combatente da vez, aplica o
/// dano dos status (veneno etc.), envelhece os status, regenera stamina e
/// avança a iniciativa pulando derrotados.
/// </summary>
public sealed class TurnSystem
{
    private readonly BattleResolver _resolver;

    public event TurnEndedHandler? TurnEnded;

    public event CombatantHandler? CombatantDefeated;

    /// <summary>
    /// Recebe o resolver (não a batalha) para reusar o anúncio único de fim de
    /// batalha — um veneno pode encerrar o combate fora de uma ação.
    /// </summary>
    public TurnSystem(BattleResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>Encerra o turno do combatente da vez.</summary>
    public void EndTurn()
    {
        var battle = _resolver.Battle;
        if (battle.IsOver)
            throw new InvalidOperationException($"Batalha '{battle.Id}' já terminou");

        var combatant = battle.Active;

        // 1. Dano por turno (veneno, sangramento) dos status ativos.
        float dot = combatant.PendingTurnDamage;
        if (dot > 0f)
        {
            combatant.ApplyDamage(dot);
            if (combatant.IsDefeated)
                CombatantDefeated?.Invoke(combatant);
        }

        // 2. Status envelhecem em turnos; expirados saem.
        combatant.TickStatuses();

        // 3. Regen de stamina (só para quem segue em pé).
        if (!combatant.IsDefeated)
            combatant.RegenStamina(CombatPhysics.StaminaRegenPerTurn);

        TurnEnded?.Invoke(battle, combatant);

        // 4. O veneno pode ter decidido a batalha; senão, próxima iniciativa.
        _resolver.AnnounceEndIfOver();
        if (!battle.IsOver)
            battle.AdvanceTurn();
    }
}
