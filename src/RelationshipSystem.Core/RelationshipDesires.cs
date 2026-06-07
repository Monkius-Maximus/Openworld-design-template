namespace RelationshipSystem.Core;

/// <summary>
/// Fábrica de Wants &amp; Fears comuns de relacionamento. São apenas atalhos para
/// montar <see cref="Desire"/>s; qualquer condição customizada também é válida.
/// </summary>
public static class RelationshipDesires
{
    /// <summary>Want: ficar amigo do alvo.</summary>
    public static Desire BefriendWant(string target, int value = 25) => new()
    {
        Id = $"want.friend.{target}",
        Description = $"Ficar amigo de {target}",
        Kind = DesireKind.Want,
        TargetId = target,
        AspirationValue = value,
        IsMet = (m, owner, t) =>
            m.TryGet(owner, t, out var r) && r!.Flags.Contains(RelationshipFlag.Friend),
    };

    /// <summary>Want: apaixonar-se pelo alvo.</summary>
    public static Desire FallInLoveWant(string target, int value = 50) => new()
    {
        Id = $"want.love.{target}",
        Description = $"Apaixonar-se por {target}",
        Kind = DesireKind.Want,
        TargetId = target,
        AspirationValue = value,
        IsMet = (m, owner, t) =>
            m.TryGet(owner, t, out var r) && r!.Flags.Contains(RelationshipFlag.Love),
    };

    /// <summary>Fear: virar inimigo do alvo.</summary>
    public static Desire EnemyFear(string target, int value = 30) => new()
    {
        Id = $"fear.enemy.{target}",
        Description = $"Virar inimigo de {target}",
        Kind = DesireKind.Fear,
        TargetId = target,
        AspirationValue = value,
        IsMet = (m, owner, t) =>
            m.TryGet(owner, t, out var r) && r!.Flags.Contains(RelationshipFlag.Enemy),
    };

    /// <summary>
    /// Fear: amar sem ser correspondido — o dono ama o alvo, mas o alvo não
    /// retribui (romance lifetime do alvo abaixo do limiar de amor).
    /// </summary>
    public static Desire UnrequitedLoveFear(string target, int value = 20) => new()
    {
        Id = $"fear.unrequited.{target}",
        Description = $"Amar {target} sem ser correspondido",
        Kind = DesireKind.Fear,
        TargetId = target,
        AspirationValue = value,
        IsMet = (m, owner, t) =>
            m.TryGet(owner, t, out var forward)
            && forward!.Flags.Contains(RelationshipFlag.Love)
            && (!m.TryGet(t, owner, out var back)
                || back!.EffectiveRomanceLifetime < RelationshipThresholds.Love),
    };
}
