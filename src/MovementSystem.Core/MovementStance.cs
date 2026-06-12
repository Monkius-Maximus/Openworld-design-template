namespace MovementSystem.Core;

/// <summary>Posturas de locomoção, como em Project Zomboid.</summary>
public enum MovementStance
{
    Sneak,
    Walk,
    Run,
    Sprint,
}

/// <summary>Resolve postura e velocidade a partir do estado dos botões.</summary>
public static class StanceResolver
{
    /// <summary>
    /// Prioridades: sneak vence tudo (agachado não se corre); mirar limita a
    /// Walk (em PZ não se corre de arma erguida); sprint vence run.
    /// </summary>
    public static MovementStance Resolve(bool sneak, bool run, bool sprint, bool aiming)
    {
        if (sneak) return MovementStance.Sneak;
        if (aiming) return MovementStance.Walk;
        if (sprint) return MovementStance.Sprint;
        if (run) return MovementStance.Run;
        return MovementStance.Walk;
    }

    public static float SpeedFor(MovementStance stance, bool aiming = false) => stance switch
    {
        MovementStance.Sneak => MovementThresholds.SneakSpeed,
        MovementStance.Walk => aiming ? MovementThresholds.AimWalkSpeed : MovementThresholds.WalkSpeed,
        MovementStance.Run => MovementThresholds.RunSpeed,
        MovementStance.Sprint => MovementThresholds.SprintSpeed,
        _ => MovementThresholds.WalkSpeed,
    };
}
