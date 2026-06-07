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
    private readonly CharacterRegistry? _characters;

    public event RelationshipEventHandler? FriendshipFormed;
    public event RelationshipEventHandler? FriendshipBroken;
    public event RelationshipEventHandler? BecameEnemies;
    public event RelationshipEventHandler? FellInLove;
    public event RelationshipEventHandler? HeartBroken;
    public event InteractionResultHandler? InteractionPerformed;

    /// <summary>
    /// <paramref name="characters"/> é opcional: quando fornecido, conversas com
    /// tópico são moduladas pelos interesses dos envolvidos. Sem ele, tópicos são
    /// ignorados silenciosamente.
    /// </summary>
    public InteractionResolver(RelationshipMatrix matrix, CharacterRegistry? characters = null)
    {
        _matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
        _characters = characters;
    }

    /// <summary>
    /// Executa uma interação. Falha rápido se as pré-condições não são atendidas.
    /// Retorna true se foi aceita. <paramref name="topic"/> (opcional) modula o
    /// ganho de Daily pelos interesses compartilhados (estilo The Sims 2).
    /// </summary>
    public bool Perform(string from, string to, InteractionDefinition def, string? topic = null)
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

        // Aplica deltas à trilha platônica. Um tópico de conversa soma (ou
        // subtrai) ao Daily conforme o interesse mútuo — só quando há registro
        // de personagens disponível.
        rel.Value.ApplyDaily(effect.DailyDelta + TopicBonus(from, to, topic));
        rel.Value.ApplyLifetime(effect.LifetimeDelta);

        // Aplica deltas à trilha romântica (independente da amizade).
        rel.Romance.ApplyDaily(effect.RomanceDailyDelta);
        rel.Romance.ApplyLifetime(effect.RomanceLifetimeDelta);

        // Adiciona modificador persistente, se houver. Clona para não
        // compartilhar estado mutável (RemainingHours) entre relacionamentos.
        if (effect.ResultingModifier is { } modTemplate)
        {
            rel.AddModifier(new RelationshipModifier
            {
                Name = modTemplate.Name,
                Value = modTemplate.Value,
                RemainingHours = modTemplate.RemainingHours
            });
        }

        // Adiciona sentimento autoral, se houver (clonado pelo mesmo motivo).
        if (effect.ResultingSentiment is { } sentTemplate)
        {
            rel.AddSentiment(Clone(sentTemplate));
        }

        // Atualiza flags e emite eventos.
        UpdateFlags(rel, def);

        // Evento genérico.
        InteractionPerformed?.Invoke(from, to, def, accepted);

        return accepted;
    }

    private float TopicBonus(string from, string to, string? topic)
    {
        if (string.IsNullOrWhiteSpace(topic) || _characters is null)
            return 0f;

        if (_characters.TryGet(from, out var a) && _characters.TryGet(to, out var b))
            return InterestCalculator.ConversationModifier(a, b, topic);

        return 0f;
    }

    private void UpdateFlags(Relationship rel, InteractionDefinition def)
    {
        // Amizade mútua.
        bool wasFriend = rel.Flags.Contains(RelationshipFlag.Friend);
        bool isFriendNow = _matrix.AreFriends(rel.FromId, rel.ToId);

        if (!wasFriend && isFriendNow)
        {
            rel.Flags.Add(RelationshipFlag.Friend);
            rel.AddSentiment(LongTerm(SentimentType.Close));
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
            {
                rel.AddSentiment(LongTerm(SentimentType.Bitter));
                BecameEnemies?.Invoke(rel);
            }
        }
        else
        {
            rel.Flags.Remove(RelationshipFlag.Enemy);
        }

        // Crush: lê a trilha ROMÂNTICA (daily). Só se FORMA em contexto romântico,
        // mas é removido sempre que o romance daily cai abaixo do limiar.
        if (def.IsRomantic && rel.EffectiveRomanceDaily >= RelationshipThresholds.Crush)
            rel.Flags.Add(RelationshipFlag.Crush);
        else if (rel.EffectiveRomanceDaily < RelationshipThresholds.Crush)
            rel.Flags.Remove(RelationshipFlag.Crush);

        // Amor: lê a trilha ROMÂNTICA (lifetime). Só se FORMA em contexto romântico
        // (FellInLove), mas a quebra (HeartBroken) vale para qualquer interação que
        // derrube o romance lifetime (ex.: um insulto também fere a trilha romântica).
        if (def.IsRomantic && rel.EffectiveRomanceLifetime >= RelationshipThresholds.Love)
        {
            if (rel.Flags.Add(RelationshipFlag.Love))
            {
                rel.AddSentiment(LongTerm(SentimentType.Enamored));
                FellInLove?.Invoke(rel);
            }
        }
        else if (rel.EffectiveRomanceLifetime < RelationshipThresholds.Love
                 && rel.Flags.Remove(RelationshipFlag.Love))
        {
            rel.AddSentiment(LongTerm(SentimentType.Hurt));
            HeartBroken?.Invoke(rel);
        }
    }

    private static Sentiment LongTerm(SentimentType type) =>
        new() { Type = type, Intensity = 1f, IsLongTerm = true };

    private static Sentiment Clone(Sentiment s) => new()
    {
        Type = s.Type,
        Intensity = s.Intensity,
        IsLongTerm = s.IsLongTerm,
        RemainingHours = s.RemainingHours
    };
}
