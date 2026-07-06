using EconomySystem.Core;
using EconomySystem.Core.Integration;
using EconomySystem.Core.Market;
using RelationshipSystem.Core;

namespace WorldSimulation.Core;

/// <summary>
/// Fachada que amarra os núcleos num mundo jogável: relacionamentos
/// (matriz, resolver, wants &amp; fears), economia (domicílios, contas,
/// carreiras, mercado v4) e o relógio do mundo. A camada de engine só
/// conversa com esta classe: avança minutos, executa interações e escuta o
/// log de eventos — nenhum sistema conhece a engine.
/// </summary>
public sealed class GameWorld
{
    private readonly List<Household> _households = new();
    private readonly Dictionary<string, Household> _homeByMember = new(StringComparer.OrdinalIgnoreCase);
    private readonly RelationshipDecaySystem _decay = new();
    private readonly BillsSystem _bills = new();
    private readonly EconomyTickSystem _economyTick;
    private readonly PaidInteractionResolver _paid;

    public CharacterRegistry Characters { get; } = new();
    public RelationshipMatrix Relationships { get; } = new();
    public InteractionResolver Interactions { get; }
    public WantsAndFearsSystem WantsAndFears { get; } = new();

    /// <summary>Mercado v4 (dono do calendário). Injetável para reusar um save.</summary>
    public CurrencyMarket Market { get; }

    public WorldClock Clock { get; }
    public IReadOnlyList<Household> Households => _households;

    /// <summary>Narração legível dos acontecimentos (HUD/console assinam aqui).</summary>
    public event Action<string>? EventLogged;

    public GameWorld(CurrencyMarket? market = null, int startHour = WorldThresholds.DefaultStartHour)
    {
        Market = market ?? new CurrencyMarket();
        Interactions = new InteractionResolver(Relationships, Characters);
        WantsAndFears.AttachTo(Interactions, Relationships);
        _economyTick = new EconomyTickSystem(_bills);
        _paid = new PaidInteractionResolver(Interactions);

        Clock = new WorldClock(startHour);
        Clock.NormalizationDue += () => _decay.NormalizationTick(Relationships);
        Clock.DayElapsed += OnDayElapsed;

        WireEventLog();
    }

    // ---- montagem do mundo ----

    public void AddHousehold(Household household)
    {
        ArgumentNullException.ThrowIfNull(household);
        if (_households.Any(h => string.Equals(h.Id, household.Id, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Household '{household.Id}' already added.");
        _households.Add(household);
    }

    /// <summary>
    /// Registra um personagem e, opcionalmente, o vincula a um domicílio já
    /// adicionado (moradia é pré-requisito para interações pagas).
    /// </summary>
    public void AddCharacter(CharacterTraits traits, string? householdId = null)
    {
        ArgumentNullException.ThrowIfNull(traits);
        Characters.Add(traits);

        if (householdId is null)
            return;

        var home = _households.FirstOrDefault(
                h => string.Equals(h.Id, householdId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown household '{householdId}'.");
        home.MemberIds.Add(traits.Id);
        _homeByMember[traits.Id] = home;
    }

    public Household? HouseholdOf(string characterId) =>
        _homeByMember.TryGetValue(characterId ?? string.Empty, out var home) ? home : null;

    // ---- passagem de tempo ----

    /// <summary>Avança o mundo em minutos de jogo (a engine converte de delta real).</summary>
    public void AdvanceMinutes(float gameMinutes) => Clock.Advance(gameMinutes);

    private void OnDayElapsed()
    {
        // O tick v4 avança calendário + inflação e usa o próprio calendário
        // como fonte do dia da semana (salários, contas, assinaturas, folha).
        _economyTick.DailyTick(_households, Market);
        _decay.DailyTick(Relationships);
        Log($"— Dia {Market.Calendar.CurrentDay} ({DayName(Market.Calendar.DayOfWeek)}) começou —");
    }

    // ---- interações (a UI chama estes; indisponível vira Outcome, não exceção) ----

    public InteractionOutcome Perform(string from, string to, InteractionDefinition def, string? topic = null)
    {
        ArgumentNullException.ThrowIfNull(def);

        var rel = Relationships.Get(from, to);
        if (!def.Available(rel))
            return InteractionOutcome.Unavailable(
                $"'{def.DisplayName}' está indisponível agora ({NameOf(from)} → {NameOf(to)}).");

        bool accepted = Interactions.Perform(from, to, def, topic);
        return new InteractionOutcome(true, accepted,
            accepted ? $"{NameOf(to)} aceitou: {def.DisplayName}." : $"{NameOf(to)} recusou: {def.DisplayName}.");
    }

    /// <summary>
    /// Presente pago (integração relacionamento ↔ economia): valida domicílio
    /// e saldo ANTES de delegar, para que "sem dinheiro" seja um Outcome claro
    /// em vez do evento Cancelled do resolver.
    /// </summary>
    public InteractionOutcome GiveGift(string from, string to)
    {
        var home = HouseholdOf(from);
        if (home is null)
            return InteractionOutcome.Unavailable($"{NameOf(from)} não pertence a um domicílio.");

        var def = RelationshipSystem.Core.Interactions.InteractionLibrary.GiveGift;
        var rel = Relationships.Get(from, to);
        if (!def.Available(rel))
            return InteractionOutcome.Unavailable(
                $"'{def.DisplayName}' está indisponível agora ({NameOf(from)} → {NameOf(to)}).");

        if (home.Funds.Balance < SocialCosts.GiftCost)
            return InteractionOutcome.Unavailable(
                $"Sem dinheiro para o presente ({MarketRules.BaseCurrencySymbol}{SocialCosts.GiftCost}).");

        bool accepted = _paid.GiveGift(home.Id, home.Funds, from, to);
        return new InteractionOutcome(true, accepted,
            $"Presente entregue a {NameOf(to)} (-{MarketRules.BaseCurrencySymbol}{SocialCosts.GiftCost}).");
    }

    // ---- narração ----

    private void WireEventLog()
    {
        Interactions.InteractionPerformed += (from, to, def, accepted) =>
            Log($"{NameOf(from)} → {NameOf(to)}: {def.DisplayName} ({(accepted ? "aceita" : "recusada")}).");
        Interactions.FriendshipFormed += rel =>
            Log($"{NameOf(rel.FromId)} e {NameOf(rel.ToId)} agora são amigos!");
        Interactions.FriendshipBroken += rel =>
            Log($"A amizade entre {NameOf(rel.FromId)} e {NameOf(rel.ToId)} acabou.");
        Interactions.BecameEnemies += rel =>
            Log($"{NameOf(rel.FromId)} agora considera {NameOf(rel.ToId)} um inimigo.");
        Interactions.FellInLove += rel =>
            Log($"{NameOf(rel.FromId)} se apaixonou por {NameOf(rel.ToId)}!");
        Interactions.HeartBroken += rel =>
            Log($"{NameOf(rel.FromId)} está de coração partido por {NameOf(rel.ToId)}.");

        WantsAndFears.WantFulfilled += (owner, desire) =>
            Log($"Desejo realizado — {NameOf(owner)}: {desire.Description} (+{desire.AspirationValue} aspiração).");
        WantsAndFears.FearRealized += (owner, desire) =>
            Log($"Medo concretizado — {NameOf(owner)}: {desire.Description} (-{desire.AspirationValue} aspiração).");

        _bills.BillDelivered += (householdId, amountDue, paid) =>
        {
            if (amountDue > 0)
                Log($"Conta de {MarketRules.BaseCurrencySymbol}{amountDue} em '{householdId}' " +
                    $"({(paid ? "paga" : "SEM FUNDOS")}).");
        };
        _bills.RepoManDispatched += householdId =>
            Log($"O repo-man visitou '{householdId}' e levou um bem!");

        _paid.Spent += (householdId, tx) =>
            Log($"'{householdId}' gastou {MarketRules.BaseCurrencySymbol}{-tx.Amount}: {tx.Reason}.");
    }

    private void Log(string message) => EventLogged?.Invoke(message);

    private string NameOf(string id) =>
        Characters.TryGet(id, out var traits) ? traits.Name : id;

    public static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Seg",
        DayOfWeek.Tuesday => "Ter",
        DayOfWeek.Wednesday => "Qua",
        DayOfWeek.Thursday => "Qui",
        DayOfWeek.Friday => "Sex",
        DayOfWeek.Saturday => "Sáb",
        _ => "Dom",
    };
}
