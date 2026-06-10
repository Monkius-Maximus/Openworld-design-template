using RelationshipSystem.Core;

namespace BattleSystem.Core.Integration;

/// <summary>
/// Costura única entre a batalha e o núcleo de relacionamentos (espelha o
/// <c>RelationshipEconomyBridge</c>): só LÊ o <see cref="RelationshipMatrix"/>
/// e ESCREVE de volta pelas APIs públicas (<c>AddModifier</c>). O núcleo de
/// relacionamentos permanece intocado e livre de dependências.
///
/// Antes da batalha, a matriz vira MORAL (lutar ao lado de um amigo anima;
/// enfrentar um rival enfurece). Depois, o desfecho vira MEMÓRIA
/// (modificadores temporários, mecanismo CK3 já existente).
/// </summary>
public sealed class RelationshipBattleBridge
{
    private readonly RelationshipMatrix _matrix;

    public RelationshipBattleBridge(RelationshipMatrix matrix)
    {
        _matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
    }

    /// <summary>
    /// Aplica a moral pré-batalha como efeitos de status que duram a batalha
    /// inteira: +ataque por lutar ao lado de um amigo (regra mútua
    /// <see cref="RelationshipMatrix.AreFriends"/>) e fúria (+ataque, -defesa)
    /// quando há um rival (daily ≤ <see cref="RelationshipThresholds.Enemy"/>)
    /// do outro lado.
    /// </summary>
    public void ApplyPreBattleMorale(Battle battle)
    {
        ArgumentNullException.ThrowIfNull(battle);

        foreach (var combatant in battle.Initiative)
        {
            if (battle.AlliesOf(combatant).Any(a => _matrix.AreFriends(combatant.Id, a.Id)))
            {
                combatant.AddStatus(new StatusEffect
                {
                    Name = MoraleRules.FriendMoraleName,
                    AttackBonus = MoraleRules.FriendAttackBonus,
                    RemainingTurns = CombatPhysics.BattleLongTurns,
                });
            }

            if (battle.EnemiesOf(combatant).Any(e =>
                    _matrix.Get(combatant.Id, e.Id).EffectiveDaily <= RelationshipThresholds.Enemy))
            {
                combatant.AddStatus(new StatusEffect
                {
                    Name = MoraleRules.RivalFuryName,
                    AttackBonus = MoraleRules.RivalAttackBonus,
                    DefenseBonus = MoraleRules.RivalDefensePenalty,
                    RemainingTurns = CombatPhysics.BattleLongTurns,
                });
            }
        }
    }

    /// <summary>
    /// Escreve o desfecho na matriz: cada perdedor guarda uma mágoa temporária
    /// de cada vencedor ("Fui derrotado em combate") e os vencedores aliados
    /// trocam um modificador mútuo ("Lutamos lado a lado"). Exige a batalha
    /// encerrada (falha rápido).
    /// </summary>
    public void ApplyPostBattleOutcome(Battle battle)
    {
        ArgumentNullException.ThrowIfNull(battle);
        if (battle.Winner is not { } winner)
            throw new InvalidOperationException($"Batalha '{battle.Id}' ainda não terminou");

        var winners = battle.Members(winner);
        var losers = battle.Members(winner == BattleTeam.A ? BattleTeam.B : BattleTeam.A);

        foreach (var loser in losers)
        foreach (var victor in winners)
        {
            _matrix.Get(loser.Id, victor.Id).AddModifier(new RelationshipModifier
            {
                Name = MoraleRules.DefeatModifierName,
                Value = MoraleRules.DefeatModifierValue,
                RemainingHours = MoraleRules.DefeatModifierHours,
            });
        }

        foreach (var a in winners)
        foreach (var b in winners)
        {
            if (a == b) continue;
            _matrix.Get(a.Id, b.Id).AddModifier(new RelationshipModifier
            {
                Name = MoraleRules.ComradeModifierName,
                Value = MoraleRules.ComradeModifierValue,
                RemainingHours = MoraleRules.ComradeModifierHours,
            });
        }
    }
}
