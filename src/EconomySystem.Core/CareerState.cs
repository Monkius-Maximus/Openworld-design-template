namespace EconomySystem.Core;

/// <summary>
/// Emprego de UM personagem: a carreira em que está e o nível atual. Estado
/// mutável por instância, no espírito do <c>Relationship</c>. Carrega a
/// referência à <see cref="CareerDefinition"/> para que o salário e os
/// requisitos sejam derivados sem um registro externo.
/// </summary>
public sealed class CareerState
{
    private readonly string _characterId = string.Empty;

    public required string CharacterId
    {
        get => _characterId;
        init => _characterId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("CharacterId required", nameof(CharacterId))
            : value;
    }

    /// <summary>Carreira atual.</summary>
    public required CareerDefinition Career { get; init; }

    /// <summary>Nível atual (começa em 1).</summary>
    public int CurrentLevel { get; private set; } = 1;

    /// <summary>Cargo atual.</summary>
    public string Title => Career.LevelAt(CurrentLevel)?.Title ?? string.Empty;

    /// <summary>Salário diário do nível atual.</summary>
    public int DailyWage => Career.LevelAt(CurrentLevel)?.DailyWage ?? 0;

    /// <summary>Já está no topo?</summary>
    public bool IsAtTop => CurrentLevel >= Career.MaxLevel;

    /// <summary>
    /// Atende aos requisitos do PRÓXIMO nível? (habilidades + amigos + humor,
    /// estilo The Sims 2). Retorna false se já está no topo.
    /// </summary>
    public bool EligibleForPromotion(CareerContext ctx)
    {
        var next = Career.LevelAt(CurrentLevel + 1);
        if (next is null)
            return false;

        if (ctx.Mood < next.RequiredMood)
            return false;
        if (ctx.FriendCount < next.RequiredFriends)
            return false;

        foreach (var (skill, required) in next.RequiredSkills)
        {
            ctx.Skills.TryGetValue(skill, out int have);
            if (have < required)
                return false;
        }
        return true;
    }

    /// <summary>Sobe um nível. Lança se já está no topo.</summary>
    public void Promote()
    {
        if (IsAtTop)
            throw new InvalidOperationException(
                $"{CharacterId} já está no topo de '{Career.Id}'.");
        CurrentLevel++;
    }

    /// <summary>Desce um nível (não passa do nível 1). Usado por chance cards (v3).</summary>
    public void Demote()
    {
        if (CurrentLevel > 1)
            CurrentLevel--;
    }
}
