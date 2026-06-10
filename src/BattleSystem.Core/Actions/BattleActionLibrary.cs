namespace BattleSystem.Core.Actions;

/// <summary>
/// Catálogo de ações de combate prontas (espelha <c>InteractionLibrary</c>).
/// Nenhuma ação depende de asset: tudo é dado + predicado puro; animação,
/// som e VFX são responsabilidade do adaptador (Godot) quando existirem.
/// </summary>
public static class BattleActionLibrary
{
    public static readonly BattleActionDefinition QuickStrike = new()
    {
        Id = "QuickStrike",
        DisplayName = "Golpe Rápido",
        StaminaCost = 10f,
        Target = ActionTarget.Enemy,
        Available = (_, target) => !target.IsDefeated,
        HitChance = (_, _) => 0.9,
        OnHit = new BattleActionEffect(Damage: 8f),
        OnMiss = new BattleActionEffect(),
    };

    public static readonly BattleActionDefinition HeavyBlow = new()
    {
        Id = "HeavyBlow",
        DisplayName = "Golpe Pesado",
        StaminaCost = 25f,
        Target = ActionTarget.Enemy,
        Available = (_, target) => !target.IsDefeated,
        HitChance = (_, _) => 0.65,
        OnHit = new BattleActionEffect(Damage: 18f),
        // Errar o golpe pesado deixa o atacante aberto por um turno.
        OnMiss = new BattleActionEffect(SelfStatus: new StatusEffect
        {
            Name = "Desequilibrado",
            DefenseBonus = -5f,
            RemainingTurns = 1,
        }),
    };

    public static readonly BattleActionDefinition PoisonedBlade = new()
    {
        Id = "PoisonedBlade",
        DisplayName = "Lâmina Envenenada",
        StaminaCost = 15f,
        Target = ActionTarget.Enemy,
        Available = (_, target) => !target.IsDefeated,
        HitChance = (_, _) => 0.75,
        OnHit = new BattleActionEffect(Damage: 4f, TargetStatus: new StatusEffect
        {
            Name = "Envenenado",
            DamagePerTurn = 3f,
            RemainingTurns = 3,
        }),
        OnMiss = new BattleActionEffect(),
    };

    public static readonly BattleActionDefinition Guard = new()
    {
        Id = "Guard",
        DisplayName = "Defender",
        StaminaCost = 0f,
        Target = ActionTarget.Self,
        Available = (_, _) => true,
        HitChance = (_, _) => 1.0,
        OnHit = new BattleActionEffect(SelfStatus: new StatusEffect
        {
            Name = "Em guarda",
            DefenseBonus = 8f,
            RemainingTurns = 1,
        }),
        OnMiss = new BattleActionEffect(),
    };

    public static readonly BattleActionDefinition Taunt = new()
    {
        Id = "Taunt",
        DisplayName = "Provocar",
        StaminaCost = 5f,
        Target = ActionTarget.Enemy,
        Available = (_, target) => !target.IsDefeated,
        HitChance = (_, _) => 0.8,
        OnHit = new BattleActionEffect(TargetStatus: new StatusEffect
        {
            Name = "Provocado",
            DefenseBonus = -4f,
            RemainingTurns = 2,
        }),
        OnMiss = new BattleActionEffect(),
    };

    public static readonly BattleActionDefinition FirstAid = new()
    {
        Id = "FirstAid",
        DisplayName = "Primeiros Socorros",
        StaminaCost = 15f,
        Target = ActionTarget.Ally,
        // Só vale a pena (e só está disponível) em alvo ferido e vivo.
        Available = (_, target) => !target.IsDefeated && target.Health < target.Stats.MaxHealth,
        HitChance = (_, _) => 1.0,
        OnHit = new BattleActionEffect(Heal: 20f),
        OnMiss = new BattleActionEffect(),
    };

    public static readonly BattleActionDefinition Rest = new()
    {
        Id = "Rest",
        DisplayName = "Descansar",
        StaminaCost = 0f,
        Target = ActionTarget.Self,
        Available = (_, _) => true,
        HitChance = (_, _) => 1.0,
        OnHit = new BattleActionEffect(StaminaRestore: 30f),
        OnMiss = new BattleActionEffect(),
    };

    /// <summary>Todas as ações registradas, indexadas por Id.</summary>
    public static readonly IReadOnlyDictionary<string, BattleActionDefinition> All =
        new[] { QuickStrike, HeavyBlow, PoisonedBlade, Guard, Taunt, FirstAid, Rest }
            .ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
}
