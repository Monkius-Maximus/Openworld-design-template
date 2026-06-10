namespace BattleSystem.Core;

public enum BattleTeam { A, B }

/// <summary>
/// Estado de um encontro por turnos entre dois times. Guarda a ordem de
/// iniciativa (velocidade decrescente, empate por id) e o ponteiro do turno;
/// quem MOVE o estado são o <c>BattleResolver</c> (ações) e o
/// <c>TurnSystem</c> (passagem de turno) — mesma separação
/// matriz/resolver/decay do núcleo de relacionamentos.
/// </summary>
public sealed class Battle
{
    public string Id { get; }

    public IReadOnlyList<Combatant> TeamA { get; }

    public IReadOnlyList<Combatant> TeamB { get; }

    /// <summary>Rodada atual (1-based); avança quando a iniciativa dá a volta.</summary>
    public int Round { get; private set; } = 1;

    private readonly List<Combatant> _initiative;
    private int _turnIndex;

    /// <summary>Sinaliza que o evento de fim já foi emitido (resolver OU turno).</summary>
    internal bool EndAnnounced { get; set; }

    public Battle(string id, IEnumerable<Combatant> teamA, IEnumerable<Combatant> teamB)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Battle id required", nameof(id));
        ArgumentNullException.ThrowIfNull(teamA);
        ArgumentNullException.ThrowIfNull(teamB);

        Id = id;
        TeamA = teamA.ToList();
        TeamB = teamB.ToList();

        if (TeamA.Count == 0 || TeamB.Count == 0)
            throw new ArgumentException("Both teams need at least one combatant");

        var duplicated = TeamA.Concat(TeamB)
            .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicated is not null)
            throw new ArgumentException($"Duplicated combatant id: {duplicated.Key}");

        _initiative = TeamA.Concat(TeamB)
            .OrderByDescending(c => c.Stats.Speed)
            .ThenBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Todos os combatentes, na ordem de iniciativa.</summary>
    public IReadOnlyList<Combatant> Initiative => _initiative;

    /// <summary>Combatente da vez.</summary>
    public Combatant Active => _initiative[_turnIndex];

    public bool IsOver => TeamA.All(c => c.IsDefeated) || TeamB.All(c => c.IsDefeated);

    /// <summary>Time vencedor, ou null enquanto a batalha corre.</summary>
    public BattleTeam? Winner => !IsOver
        ? null
        : TeamA.Any(c => !c.IsDefeated) ? BattleTeam.A : BattleTeam.B;

    public Combatant Find(string combatantId)
    {
        var found = _initiative.FirstOrDefault(
            c => string.Equals(c.Id, combatantId, StringComparison.OrdinalIgnoreCase));
        return found ?? throw new KeyNotFoundException($"Combatant '{combatantId}' is not in battle '{Id}'");
    }

    public BattleTeam TeamOf(Combatant combatant)
    {
        ArgumentNullException.ThrowIfNull(combatant);
        if (TeamA.Contains(combatant)) return BattleTeam.A;
        if (TeamB.Contains(combatant)) return BattleTeam.B;
        throw new ArgumentException($"Combatant '{combatant.Id}' is not in battle '{Id}'");
    }

    public IReadOnlyList<Combatant> Members(BattleTeam team) =>
        team == BattleTeam.A ? TeamA : TeamB;

    /// <summary>Aliados vivos de um combatente (exclui ele próprio).</summary>
    public IEnumerable<Combatant> AlliesOf(Combatant combatant) =>
        Members(TeamOf(combatant)).Where(c => c != combatant && !c.IsDefeated);

    /// <summary>Inimigos vivos de um combatente.</summary>
    public IEnumerable<Combatant> EnemiesOf(Combatant combatant) =>
        Members(TeamOf(combatant) == BattleTeam.A ? BattleTeam.B : BattleTeam.A)
            .Where(c => !c.IsDefeated);

    /// <summary>
    /// Move o ponteiro para o próximo combatente VIVO; ao dar a volta, avança a
    /// rodada. Chamado pelo <c>TurnSystem</c>; não chame se a batalha acabou.
    /// </summary>
    public void AdvanceTurn()
    {
        if (IsOver)
            throw new InvalidOperationException($"Battle '{Id}' is over");

        do
        {
            _turnIndex++;
            if (_turnIndex >= _initiative.Count)
            {
                _turnIndex = 0;
                Round++;
            }
        } while (Active.IsDefeated);
    }
}
