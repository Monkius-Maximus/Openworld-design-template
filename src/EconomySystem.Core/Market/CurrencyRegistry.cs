namespace EconomySystem.Core.Market;

/// <summary>
/// Registro de moedas por Id (case-insensitive). Espelha o
/// <c>CharacterRegistry</c>: store com dicionário e acesso fail-fast. A moeda
/// base $Money é semeada no construtor e nunca pode ser removida.
/// </summary>
public sealed class CurrencyRegistry
{
    private readonly Dictionary<string, Currency> _byId =
        new(StringComparer.OrdinalIgnoreCase);

    public CurrencyRegistry(Currency? baseCurrency = null)
    {
        if (baseCurrency is not null && !baseCurrency.IsBase)
            throw new ArgumentException(
                "Seed currency must have IsBase = true", nameof(baseCurrency));

        Base = baseCurrency ?? Currency.CreateBase();
        _byId[Base.Id] = Base;
    }

    /// <summary>A moeda canônica $Money.</summary>
    public Currency Base { get; }

    /// <summary>Todas as moedas registradas (inclui a base).</summary>
    public IReadOnlyCollection<Currency> All => _byId.Values;

    public int Count => _byId.Count;

    /// <summary>Disparado quando uma moeda nova entra no registro.</summary>
    public event Action<Currency>? CurrencyAdded;

    /// <summary>Disparado quando uma moeda é removida do registro.</summary>
    public event Action<Currency>? CurrencyRemoved;

    public bool TryGet(string id, out Currency currency)
    {
        if (id is not null && _byId.TryGetValue(id, out var found))
        {
            currency = found;
            return true;
        }
        currency = null!;
        return false;
    }

    public Currency Get(string id) =>
        TryGet(id, out var c)
            ? c
            : throw new KeyNotFoundException($"Moeda '{id}' não registrada.");

    /// <summary>
    /// Registra uma moeda nova. Falha se o Id já existir ou se a moeda se
    /// declarar base (só existe uma base, semeada no construtor).
    /// </summary>
    public void Add(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        if (currency.IsBase)
            throw new ArgumentException(
                "A moeda base já existe; novas moedas devem ter IsBase = false.",
                nameof(currency));
        if (_byId.ContainsKey(currency.Id))
            throw new ArgumentException(
                $"Moeda '{currency.Id}' já registrada.", nameof(currency));

        _byId[currency.Id] = currency;
        CurrencyAdded?.Invoke(currency);
    }

    /// <summary>
    /// Remove uma moeda pelo Id. Retorna false se não existir; lança se for a
    /// moeda base (ela é o referencial de todo o motor).
    /// </summary>
    public bool Remove(string id)
    {
        if (!TryGet(id, out var currency))
            return false;
        if (currency.IsBase)
            throw new InvalidOperationException(
                "A moeda base $Money não pode ser removida.");

        _byId.Remove(currency.Id);
        CurrencyRemoved?.Invoke(currency);
        return true;
    }
}
