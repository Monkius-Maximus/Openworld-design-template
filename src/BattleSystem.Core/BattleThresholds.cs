namespace BattleSystem.Core;

/// <summary>
/// Limiares de combate. Tunáveis por projeto (espelha <c>RelationshipThresholds</c>).
/// No Godot, estes valores podem ser expostos depois via <c>Resource</c> exportado;
/// o núcleo permanece agnóstico de engine.
/// </summary>
public static class BattleThresholds
{
    /// <summary>Fração de vida abaixo da qual um combatente é considerado "em perigo".</summary>
    public const float LowHealthFraction = 0.25f;
}

/// <summary>
/// Constantes da passagem de turnos (espelha <c>RelationshipPhysics</c>).
/// </summary>
public static class CombatPhysics
{
    /// <summary>Dano mínimo de um golpe que acerta (a defesa nunca zera o golpe).</summary>
    public const float MinDamage = 1f;

    /// <summary>Stamina recuperada automaticamente ao fim do próprio turno.</summary>
    public const float StaminaRegenPerTurn = 5f;

    /// <summary>Duração "pela batalha inteira" para efeitos de moral pré-batalha.</summary>
    public const int BattleLongTurns = 999;
}

/// <summary>
/// Pesos da ponte batalha ↔ relacionamentos (espelha <c>AttractionWeights</c>):
/// o que a moral pré-batalha vale e o que o desfecho escreve de volta na matriz.
/// </summary>
public static class MoraleRules
{
    /// <summary>Nome do efeito de lutar ao lado de um amigo (mútuo, via <c>AreFriends</c>).</summary>
    public const string FriendMoraleName = "Ao lado de um amigo";
    public const float FriendAttackBonus = 5f;

    /// <summary>Nome do efeito de enfrentar um rival (daily ≤ Enemy).</summary>
    public const string RivalFuryName = "Fúria contra rival";
    public const float RivalAttackBonus = 8f;
    public const float RivalDefensePenalty = -4f;

    /// <summary>Modificador escrito no relacionamento perdedor → vencedor.</summary>
    public const string DefeatModifierName = "Fui derrotado em combate";
    public const float DefeatModifierValue = -15f;
    public const float DefeatModifierHours = 48f;

    /// <summary>Modificador mútuo entre vencedores aliados.</summary>
    public const string ComradeModifierName = "Lutamos lado a lado";
    public const float ComradeModifierValue = 10f;
    public const float ComradeModifierHours = 72f;
}
