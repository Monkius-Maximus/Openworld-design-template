namespace RelationshipSystem.Core;

public enum RelationshipFlag { Friend, BestFriend, Enemy, Crush, Love }

/// <summary>
/// Relacionamento de uma direção: A → B.
/// A assimetria é intencional: A→B pode diferir de B→A
/// (paixão não-correspondida, mágoa unilateral).
/// </summary>
public sealed class Relationship
{
    /// <summary>Nome do modificador de fúria gerado por <c>Insult</c>.</summary>
    public const string FuryModifierName = "Foi insultado";

    private readonly string _fromId = string.Empty;
    private readonly string _toId = string.Empty;

    public required string FromId
    {
        get => _fromId;
        init => _fromId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("FromId required", nameof(FromId))
            : value;
    }

    public required string ToId
    {
        get => _toId;
        init => _toId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("ToId required", nameof(ToId))
            : value;
    }

    /// <summary>Valores: daily e lifetime.</summary>
    public RelationshipValue Value { get; } = new();

    /// <summary>Score de atração (separado, eixo diferente).</summary>
    public int AttractionScore { get; set; } = 0;

    /// <summary>Flags de relacionamento (Friend, Enemy, Love, etc).</summary>
    public HashSet<RelationshipFlag> Flags { get; } = new();

    /// <summary>Modificadores nomeados (turnos, presentes, ofensas, etc).</summary>
    public List<RelationshipModifier> Modifiers { get; } = new();

    private readonly List<Sentiment> _sentiments = new();

    /// <summary>
    /// Sentimentos persistentes de A por B (estilo The Sims 4). Camada narrativa
    /// separada do score: não entram no <see cref="EffectiveDaily"/>.
    /// Limitado a <see cref="SentimentDefaults.MaxSentiments"/>; o mais fraco sai.
    /// </summary>
    public IReadOnlyList<Sentiment> Sentiments => _sentiments;

    /// <summary>Score efetivo = Value + soma dos modificadores (ignora expirados).</summary>
    public float EffectiveDaily => Value.Daily + ModifierSum();

    public float EffectiveLifetime => Value.Lifetime + ModifierSum();

    /// <summary>True enquanto houver um modificador de fúria ativo.</summary>
    public bool IsFurious =>
        Modifiers.Any(m => !m.IsExpired && m.Name == FuryModifierName);

    private float ModifierSum() =>
        Modifiers.Where(m => !m.IsExpired).Sum(m => m.Value);

    /// <summary>Decai modificadores; remove os expirados.</summary>
    public void DecayModifiers(float hoursPassed)
    {
        Modifiers.RemoveAll(m => m.DecayTime(hoursPassed));
    }

    /// <summary>Adiciona um modificador (ex: "foi ofendido").</summary>
    public void AddModifier(RelationshipModifier mod)
    {
        ArgumentNullException.ThrowIfNull(mod);
        Modifiers.Add(mod);
    }

    /// <summary>
    /// Adiciona um sentimento. Se já existe um do mesmo <see cref="SentimentType"/>,
    /// ele é reforçado (intensidade acumula até o teto; o prazo é estendido) em vez
    /// de duplicar. Ao exceder o limite, o sentimento mais fraco é descartado.
    /// </summary>
    public void AddSentiment(Sentiment sentiment)
    {
        ArgumentNullException.ThrowIfNull(sentiment);

        var existing = _sentiments.FirstOrDefault(s => s.Type == sentiment.Type);
        if (existing is not null)
        {
            existing.Intensity = Math.Min(
                existing.Intensity + sentiment.Intensity, SentimentDefaults.MaxIntensity);
            existing.RemainingHours = Math.Max(existing.RemainingHours, sentiment.RemainingHours);
            return;
        }

        _sentiments.Add(sentiment);

        // Mantém apenas os mais fortes (estilo TS4: o 5º expulsa o mais fraco).
        while (_sentiments.Count > SentimentDefaults.MaxSentiments)
        {
            var weakest = _sentiments
                .Aggregate((a, b) => b.Intensity < a.Intensity ? b : a);
            _sentiments.Remove(weakest);
        }
    }

    /// <summary>Envelhece sentimentos de curto prazo; remove os expirados.</summary>
    public void DecaySentiments(float hoursPassed)
    {
        _sentiments.RemoveAll(s => s.DecayTime(hoursPassed));
    }
}
