namespace EconomySystem.Core;

/// <summary>
/// Definição declarativa de uma carreira: uma pista e seus níveis ordenados.
/// Espelha <c>InteractionDefinition</c> (schema imutável). Os níveis devem ser
/// contíguos a partir de 1 (validação fail-fast no <c>init</c>).
/// </summary>
public sealed class CareerDefinition
{
    private readonly IReadOnlyList<CareerLevel> _levels = Array.Empty<CareerLevel>();

    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required CareerTrack Track { get; init; }

    /// <summary>Níveis da carreira, do 1 (estagiário) ao topo.</summary>
    public required IReadOnlyList<CareerLevel> Levels
    {
        get => _levels;
        init
        {
            if (value is null || value.Count == 0)
                throw new ArgumentException("Career requires at least one level", nameof(Levels));
            for (int i = 0; i < value.Count; i++)
            {
                if (value[i].Level != i + 1)
                    throw new ArgumentException(
                        $"Career levels must be contiguous from 1 (level at index {i} was {value[i].Level})",
                        nameof(Levels));
            }
            _levels = value;
        }
    }

    /// <summary>Nível máximo (topo da carreira).</summary>
    public int MaxLevel => _levels.Count;

    /// <summary>Retorna o nível pedido, ou null se fora do intervalo [1..MaxLevel].</summary>
    public CareerLevel? LevelAt(int level) =>
        level >= 1 && level <= _levels.Count ? _levels[level - 1] : null;
}
