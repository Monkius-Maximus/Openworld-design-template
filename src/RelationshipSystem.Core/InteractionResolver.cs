namespace RelationshipSystem.Core;

public delegate void RelationshipEventHandler(Relationship rel);

public delegate void InteractionResultHandler(string from, string to, InteractionDefinition def, bool accepted);

/// <summary>
/// Executa uma interação entre dois personagens, aplica efeitos,
/// atualiza flags e emite eventos.
/// </summary>
public sealed class InteractionResolver
{
    private readonly RelationshipMatrix _matrix;

    public event RelationshipEventHandler? FriendshipFormed;
    public event RelationshipEventHandler? FriendshipBroken;
    public event RelationshipEventHandler? BecameEnemies;
    public event RelationshipEventHandler? FellInLove;
    public event RelationshipEventHandler? HeartBroken;
    public event InteractionResultHandler? InteractionPerformed;

    public InteractionResolver(RelationshipMatrix matrix)
    {
        _matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
    }

    /// <summary>
    /// Executa uma interação. Falha rápido se as pré-condições não são atendidas.
    /// Retorna true se foi aceita.
    /// </summary>
    public bool Perform(string from, string to, InteractionDefinition def)
    {
        ArgumentNullException.ThrowIfNull(def);

        var rel = _matrix.Get(from, to);

        // FALHA RÁPIDO
        if (!def.Available(rel))
        {
            throw new InvalidOperationException(
                $"Interação '{def.Id}' indisponível: {from} → {to}");
        }

        bool accepted = def.Accepted(rel);
        var effect = accepted ? def.OnAccept : def.OnReject;

        // Aplica deltas.
        rel.Value.ApplyDaily(effect.DailyDelta);
        rel.Value.ApplyLifetime(effect.LifetimeDelta);

        // Adiciona modificador persistente, se houver. Clona para não
        // compartilhar estado mutável (RemainingHours) entre relacionamentos.
        if (effect.ResultingModifier is { } template)
        {
            rel.AddModifier(new RelationshipModifier
            {
                Name = template.Name,
                Value = template.Value,
                RemainingHours = template.RemainingHours
            });
        }

        // Atualiza flags e emite eventos.
        UpdateFlags(rel, def);

        // Evento genérico.
        InteractionPerformed?.Invoke(from, to, def, accepted);

        return accepted;
    }

    private void UpdateFlags(Relationship rel, InteractionDefinition def)
    {
        // Amizade mútua.
        bool wasFriend = rel.Flags.Contains(RelationshipFlag.Friend);
        bool isFriendNow = _matrix.AreFriends(rel.FromId, rel.ToId);

        if (!wasFriend && isFriendNow)
        {
            rel.Flags.Add(RelationshipFlag.Friend);
            FriendshipFormed?.Invoke(rel);
        }
        else if (wasFriend && !isFriendNow)
        {
            rel.Flags.Remove(RelationshipFlag.Friend);
            FriendshipBroken?.Invoke(rel);
        }

        // Melhor amizade (lifetime mútuo).
        if (_matrix.AreBestFriends(rel.FromId, rel.ToId))
            rel.Flags.Add(RelationshipFlag.BestFriend);
        else
            rel.Flags.Remove(RelationshipFlag.BestFriend);

        // Inimigos.
        if (rel.EffectiveDaily <= RelationshipThresholds.Enemy)
        {
            if (rel.Flags.Add(RelationshipFlag.Enemy))
                BecameEnemies?.Invoke(rel);
        }
        else
        {
            rel.Flags.Remove(RelationshipFlag.Enemy);
        }

        // Crush e amor (apenas em contexto romântico).
        if (def.IsRomantic)
        {
            if (rel.EffectiveDaily >= RelationshipThresholds.Crush)
                rel.Flags.Add(RelationshipFlag.Crush);

            if (rel.EffectiveLifetime >= RelationshipThresholds.Love)
            {
                if (rel.Flags.Add(RelationshipFlag.Love))
                    FellInLove?.Invoke(rel);
            }
            else if (rel.Flags.Remove(RelationshipFlag.Love))
            {
                HeartBroken?.Invoke(rel);
            }
        }
    }
}
