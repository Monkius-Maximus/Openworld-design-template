namespace RelationshipSystem.Core;

public delegate void DesireEventHandler(string ownerId, Desire desire);

/// <summary>
/// Sistema de Wants &amp; Fears (The Sims 2). Cada personagem tem uma barra de
/// aspiração e um conjunto de desejos dinâmicos. Quando um desejo é realizado,
/// a aspiração é ajustada, um evento é emitido e o desejo (one-shot) é removido.
/// </summary>
public sealed class WantsAndFearsSystem
{
    private readonly Dictionary<string, AspirationMeter> _meters =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Desire>> _desires =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Disparado quando um Want é cumprido.</summary>
    public event DesireEventHandler? WantFulfilled;

    /// <summary>Disparado quando um Fear se concretiza.</summary>
    public event DesireEventHandler? FearRealized;

    /// <summary>Barra de aspiração do personagem (criada sob demanda).</summary>
    public AspirationMeter MeterFor(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("ownerId required", nameof(ownerId));

        if (!_meters.TryGetValue(ownerId, out var meter))
        {
            meter = new AspirationMeter();
            _meters[ownerId] = meter;
        }
        return meter;
    }

    /// <summary>Desejos ativos de um personagem.</summary>
    public IReadOnlyList<Desire> DesiresOf(string ownerId) =>
        _desires.TryGetValue(ownerId ?? string.Empty, out var list)
            ? list
            : Array.Empty<Desire>();

    /// <summary>Registra um Want ou Fear para um personagem.</summary>
    public void Add(string ownerId, Desire desire)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("ownerId required", nameof(ownerId));
        ArgumentNullException.ThrowIfNull(desire);

        MeterFor(ownerId); // garante a barra
        if (!_desires.TryGetValue(ownerId, out var list))
        {
            list = new List<Desire>();
            _desires[ownerId] = list;
        }
        list.Add(desire);
    }

    /// <summary>
    /// Reavalia todos os desejos do personagem. Os realizados ajustam a
    /// aspiração, emitem evento e são removidos.
    /// </summary>
    public void Evaluate(RelationshipMatrix matrix, string ownerId)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("ownerId required", nameof(ownerId));

        if (!_desires.TryGetValue(ownerId, out var list) || list.Count == 0)
            return;

        var meter = MeterFor(ownerId);

        // Itera sobre uma cópia: realizados são removidos da lista original.
        foreach (var desire in list.ToArray())
        {
            if (!desire.IsMet(matrix, ownerId, desire.TargetId))
                continue;

            list.Remove(desire);

            if (desire.Kind == DesireKind.Want)
            {
                meter.Apply(desire.AspirationValue);
                WantFulfilled?.Invoke(ownerId, desire);
            }
            else
            {
                meter.Apply(-desire.AspirationValue);
                FearRealized?.Invoke(ownerId, desire);
            }
        }
    }

    /// <summary>
    /// Liga-se a um resolver: após cada interação, reavalia os desejos dos dois
    /// envolvidos (quem agiu e quem recebeu).
    /// </summary>
    public void AttachTo(InteractionResolver resolver, RelationshipMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(matrix);

        resolver.InteractionPerformed += (from, to, _, _) =>
        {
            Evaluate(matrix, from);
            Evaluate(matrix, to);
        };
    }
}
