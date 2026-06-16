using EconomySystem.Core;
using EconomySystem.Core.Aspiration;
using EconomySystem.Core.Businesses;
using EconomySystem.Core.Careers;
using EconomySystem.Core.Digital;
using EconomySystem.Core.Economy;
using EconomySystem.Core.Integration;
using EconomySystem.Core.Market;
using RelationshipSystem.Core;

// ─────────────────────────────────────────────────────────────────────────────
// Demo da economia inspirada em The Sims 2 ("simplicidade amplificada").
// Mostra o loop fechado: ganhar → guardar → gastar → o tempo passa,
// e a integração com o núcleo de relacionamentos (v1–v3).
// ─────────────────────────────────────────────────────────────────────────────

static string M(int money) => $"{MarketRules.BaseCurrencySymbol}{money}";

Console.WriteLine("=== Economia: The Sims 2 (simplicidade amplificada) ===\n");

// 1. Mundo: relacionamentos + domicílio dos Costa (Alice e Bob).
var matrix = new RelationshipMatrix();
var bridge = new RelationshipEconomyBridge(matrix);

var lar = new Household { Id = "lar-costa", Funds = new HouseholdFunds(EconomyThresholds.StartingFunds) };
lar.MemberIds.Add("alice");
lar.MemberIds.Add("bob");
lar.Inventory.Add(new OwnedObject { Id = "tv", PurchasePrice = 1_200 });
lar.Inventory.Add(new OwnedObject { Id = "sofa", PurchasePrice = 800 });
lar.Subscriptions.Add(new Subscription { Name = "Streaming", Cost = 40 });
lar.Subscriptions.Add(new Subscription { Name = "Plano de celular", Cost = 60 });
lar.Careers["alice"] = new CareerState { CharacterId = "alice", Career = CareerLibrary.Business };
lar.Careers["bob"] = new CareerState { CharacterId = "bob", Career = CareerLibrary.GigCourier };

Console.WriteLine($"Caixa inicial: {M(lar.Funds.Balance)} | Patrimônio: {M(lar.NetWorth)}");
Console.WriteLine($"Alice: {CareerLibrary.Business.DisplayName} ({lar.Careers["alice"].Title}, {M(lar.Careers["alice"].DailyWage)}/dia)");
Console.WriteLine($"Bob:   {CareerLibrary.GigCourier.DisplayName} ({lar.Careers["bob"].Title}, {M(lar.Careers["bob"].DailyWage)}/dia)\n");

// 2. Resolvers e eventos.
var bills = new BillsSystem();
var careers = new CareerResolver();
var business = new BusinessResolver();
var income = new IncomeResolver();
var aspiration = new AspirationResolver();
var tick = new EconomyTickSystem(bills, careers, business);

careers.Promoted += (who, level) => Console.WriteLine($"  🎉 {who} foi promovido a {level.Title} ({M(level.DailyWage)}/dia)");
bills.RepoManDispatched += id => Console.WriteLine($"  🚚 Repo-man visitou {id}!");
business.Sold += (owner, customer, profit) => Console.WriteLine($"  💰 Venda de {owner} p/ {customer}: lucro {M(profit)}");
aspiration.Redeemed += (who, reward) => Console.WriteLine($"  ✨ {who} resgatou '{reward.Name}'");

// 3. Renda por habilidade (v1) e bico digital (v2).
Console.WriteLine("— Renda avulsa —");
income.Perform(lar.Id, lar.Funds, IncomeActivityLibrary.SellPainting, skillLevel: 6);
income.Perform(lar.Id, lar.Funds, DigitalIncomeLibrary.AppGig, skillLevel: 0);
Console.WriteLine($"Caixa após bicos: {M(lar.Funds.Balance)}\n");

// 4. Uma semana passa: salários entram, bens depreciam, contas e assinaturas saem.
Console.WriteLine("— Uma semana de ticks —");
foreach (var day in new[]
{
    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
    DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
})
{
    int before = lar.Funds.Balance;
    tick.DailyTick(lar, day);
    Console.WriteLine($"  {day,-10}: {M(before)} → {M(lar.Funds.Balance)}");
}
Console.WriteLine($"Patrimônio após a semana: {M(lar.NetWorth)}\n");

// 5. Promoção de Alice — depende de habilidades + AMIGOS (gate social do TS2).
Console.WriteLine("— Promoção (gate de amigos) —");
var skills = new Dictionary<string, int> { ["Charisma"] = 3, ["Logic"] = 2 };
Console.WriteLine($"Amigos de Alice agora: {bridge.CountFriends("alice")} → tentativa de promoção:");
bool semAmigos = bridge.EvaluatePromotion(careers, lar.Careers["alice"], skills, mood: 60);
Console.WriteLine($"  resultado sem amigos: {(semAmigos ? "promovida" : "barrada")}");

// Alice faz amizades mútuas (precisa de 3 para o nível 3 de Negócios).
foreach (var amigo in new[] { "bob", "carol", "dane" })
{
    matrix.Get("alice", amigo).Value.ApplyDaily(60f);
    matrix.Get(amigo, "alice").Value.ApplyDaily(60f);
}
Console.WriteLine($"Amigos de Alice agora: {bridge.CountFriends("alice")} → nova tentativa:");
while (bridge.EvaluatePromotion(careers, lar.Careers["alice"], skills, mood: 60)) { }
Console.WriteLine($"Cargo final de Alice: {lar.Careers["alice"].Title} (nível {lar.Careers["alice"].CurrentLevel})\n");

// 6. Negócio próprio (Open for Business, v1+v3): estoque, venda e perks.
Console.WriteLine("— Negócio próprio (boutique) —");
var loja = new Business { Id = "boutique", OwnerId = "alice", MarkupPercent = 30 };
loja.Employees.Add(new BusinessEmployee { CharacterId = "eddie", Role = EmployeeRole.Cashier });
lar.Businesses.Add(loja);

// Boutique já consolidada por vendas anteriores → ranking com perks.
loja.Loyalty.RecordSpend("clientela-histórica", 2_500);
Console.WriteLine($"Boutique consolidada: {loja.Rank}★ | perks: {string.Join(", ", loja.UnlockedPerks)}");

// Reposição usufrui do desconto de atacado (perk WholesaleDiscount).
int caixaAntes = lar.Funds.Balance;
business.Restock(loja, lar.Funds, "vestido", unitCost: 50, quantity: 6);
Console.WriteLine($"Reposição 6×{M(50)} com perk ({loja.RestockDiscountPercent}% off): {M(caixaAntes)} → {M(lar.Funds.Balance)}");

// Clientes que gostam da Alice compram com mais facilidade (cliente = relacionamento).
foreach (var cliente in new[] { "carol", "dane", "fern" })
    matrix.Get(cliente, "alice").Value.ApplyDaily(75f);
foreach (var cliente in new[] { "carol", "dane", "fern" })
    bridge.SellViaRelationship(business, loja, lar.Funds, cliente, "vestido");
Console.WriteLine($"Vendas totais da boutique: {M(loja.Loyalty.TotalSpend)} → {loja.Rank}★\n");

// 7. Chance card no trabalho (v3): duas opções, o jogador escolhe.
Console.WriteLine("— Chance card —");
var card = ChanceCardLibrary.BusinessDeal;
Console.WriteLine($"  \"{card.Prompt}\"");
Console.WriteLine($"  Escolha A: {card.OptionA.Label}");
careers.ChanceCardResolved += (who, outcome) =>
    Console.WriteLine($"  → {who}: {outcome.Label} (caixa {(outcome.FundsDelta >= 0 ? "+" : "")}{outcome.FundsDelta})");
careers.ResolveChanceCard(lar.Id, lar.Funds, lar.Careers["alice"], card, chooseA: true);
Console.WriteLine();

// 8. Moeda "soft": pontos de aspiração e objetos de recompensa (v3).
Console.WriteLine("— Aspiração (moeda paralela) —");
var aspiracaoAlice = new AspirationWallet();
aspiracaoAlice.Earn(16_000);
Console.WriteLine($"Pontos de aspiração de Alice: {aspiracaoAlice.Points}");
aspiration.TryRedeem("alice", aspiracaoAlice, AspirationRewardCatalog.MoneyTree);
Console.WriteLine($"Pontos restantes: {aspiracaoAlice.Points}\n");

// 9. Presente pago (v2): custa $Money e afeta o relacionamento.
Console.WriteLine("— Presente pago —");
var paid = new PaidInteractionResolver(new InteractionResolver(matrix));
paid.GiveGift(lar.Id, lar.Funds, "alice", "bob");
Console.WriteLine($"Alice deu um presente a Bob (custo {M(SocialCosts.GiftCost)}); " +
                  $"daily Alice→Bob = {matrix.Get("alice", "bob").EffectiveDaily}\n");

// 10. Balanço final.
Console.WriteLine("=== Balanço final ===");
Console.WriteLine($"Caixa: {M(lar.Funds.Balance)} | Bens: {M(lar.Inventory.TotalObjectValue)} | Patrimônio: {M(lar.NetWorth)}");
Console.WriteLine($"Últimas movimentações:");
foreach (var tx in lar.Funds.History.TakeLast(5))
    Console.WriteLine($"  {(tx.Amount >= 0 ? "+" : "")}{tx.Amount,-7} {tx.Kind,-13} {tx.Reason}");

// 11. Mercado financeiro (v4): moedas, câmbio e inflação.
Console.WriteLine("\n— Mercado financeiro (v4) —");
var mercado = new CurrencyMarket();
mercado.Inflation.GlobalAnnualPercent = 5m;
mercado.Currencies.Add(new Currency
{
    Id = "Dolar",
    Name = "$Dolar",
    Symbol = "$D",
    UnitsPerMoney = 5.2m,
    ProjectedAnnualInflationPercent = 10m,
    CreatedOnDay = mercado.Calendar.CurrentDay,
});

PriceQuote CotaCarro(string moeda) =>
    mercado.Quote(MarketRules.SamplePreviewPriceMoney, moeda, productId: "carro");

Console.WriteLine($"Inflação global: {mercado.Inflation.GlobalAnnualPercent}%/ano | " +
                  $"$Dolar: taxa 5.2, inflação projetada 10%/ano (ativa no dia {mercado.Currencies.Get("Dolar").InflationActivationDay})");
Console.WriteLine($"Dia {mercado.Calendar.CurrentDay,4} (ano {mercado.Calendar.CurrentYear}): " +
                  $"carro = {CotaCarro(MarketRules.BaseCurrencyId)} | {CotaCarro("Dolar")}");

for (int i = 0; i < MarketRules.DaysPerYear; i++) mercado.AdvanceDay();
Console.WriteLine($"Dia {mercado.Calendar.CurrentDay,4} (ano {mercado.Calendar.CurrentYear}): " +
                  $"carro = {CotaCarro(MarketRules.BaseCurrencyId)} | {CotaCarro("Dolar")} " +
                  $"(só deriva global — inflação do $Dolar acabou de ativar)");

for (int i = 0; i < MarketRules.DaysPerYear; i++) mercado.AdvanceDay();
Console.WriteLine($"Dia {mercado.Calendar.CurrentDay,4} (ano {mercado.Calendar.CurrentYear}): " +
                  $"carro = {CotaCarro(MarketRules.BaseCurrencyId)} | {CotaCarro("Dolar")} " +
                  $"(global + inflação própria do $Dolar)");

var salvo = MarketStateSerializer.ToJson(mercado);
var recarregado = MarketStateSerializer.FromJson(salvo);
Console.WriteLine($"Persistência: round-trip JSON ok — dia {recarregado.Calendar.CurrentDay}, " +
                  $"{recarregado.Currencies.Count} moedas, carro em $Dolar = " +
                  $"{recarregado.Quote(MarketRules.SamplePreviewPriceMoney, "Dolar", "carro")}");

// 12. Eventos econômicos + renda indexada (v5): o mercado fecha o loop com o
//     tick clássico — choques mexem na inflação E no salário real.
Console.WriteLine("\n— Eventos econômicos e renda indexada (v5) —");
var larV5 = new Household { Id = "lar-v5", Funds = new HouseholdFunds(0) };
larV5.Careers["alice"] = new CareerState { CharacterId = "alice", Career = CareerLibrary.Business };
int salarioBase = larV5.Careers["alice"].DailyWage;

var mercadoV5 = new CurrencyMarket();
mercadoV5.Inflation.GlobalAnnualPercent = 12m; // custo de vida sobe o ano todo
mercadoV5.Events.Scheduled += e =>
    Console.WriteLine($"  📅 Evento agendado: {e.Name} (dia {e.StartDay}, {e.DurationDays} dias)");
mercadoV5.Events.Schedule(EconomicEventLibrary.Boom(startDay: 30));
mercadoV5.Events.Schedule(EconomicEventLibrary.Recession(startDay: 220));

careers.WagePaid += (_, txn) =>
{
    if (txn.Reason.Contains("Negócios"))
        Console.WriteLine($"  dia {mercadoV5.Calendar.CurrentDay,3} (ano {mercadoV5.Calendar.CurrentYear}): " +
                          $"salário {M(salarioBase)} → {M(txn.Amount)} " +
                          $"(reajuste ×{mercadoV5.IncomeAdjustmentFactor():0.000})");
};

Console.WriteLine($"Salário base de Alice: {M(salarioBase)}/dia. Amostrando ao longo de um ano:");
int[] amostras = { 1, 60, 150, 230, 363 };
for (int dia = 1; dia <= MarketRules.DaysPerYear; dia++)
{
    int amostra = Array.IndexOf(amostras, dia);
    if (amostra < 0)
    {
        mercadoV5.AdvanceDay();
        continue;
    }
    tick.DailyTick(new[] { larV5 }, mercadoV5); // avança o dia, paga já indexado
}
Console.WriteLine($"Caixa de Alice após o ano (só salários, já reajustados): {M(larV5.Funds.Balance)}");

// 13. Câmbio em uso, eventos aleatórios e histórico (v6).
Console.WriteLine("\n— Gastar em moeda, eventos aleatórios e histórico (v6) —");
var historico = new MarketHistory();
var mercadoV6 = new CurrencyMarket(history: historico);
mercadoV6.Inflation.GlobalAnnualPercent = 6m;
mercadoV6.Currencies.Add(new Currency
{
    Id = "Dolar", Name = "$Dolar", Symbol = "$D",
    UnitsPerMoney = 5.2m, ProjectedAnnualInflationPercent = 15m, CreatedOnDay = 0,
});

// Gerador estocástico com semente fixa → sequência reproduzível.
var gerador = new RandomEventGenerator(seed: 2026, dailyChance: 0.02);
mercadoV6.Events.Scheduled += e =>
    Console.WriteLine($"  🎲 Choque sorteado: {e.Name} no dia {e.StartDay}");

Console.WriteLine("Simulando 2 anos com eventos aleatórios (semente 2026):");
for (int i = 0; i < MarketRules.DaysPerYear * 2; i++)
{
    mercadoV6.AdvanceDay();
    gerador.MaybeGenerate(mercadoV6);
}
var primeira = historico.Samples[0];
var ultima = historico.Latest!.Value;
Console.WriteLine($"Histórico: {historico.Samples.Count} amostras; índice global " +
                  $"{primeira.GlobalIndex:0.000} (dia {primeira.Day}) → {ultima.GlobalIndex:0.000} (dia {ultima.Day})");

// Gastar em moeda estrangeira: o carro cotado em $Dolar, debitado em $Money.
var comprador = new Household { Id = "comprador", Funds = new HouseholdFunds(50_000) };
var compras = new MarketPurchaseResolver(mercadoV6);
compras.Purchased += (_, tx) =>
    Console.WriteLine($"  🛒 {tx.Reason}: debitado {M(-tx.Amount)} do caixa");

int custoBase = mercadoV6.CostInMoney(MarketRules.SamplePreviewPriceMoney, MarketRules.BaseCurrencyId, "carro");
int custoDolar = mercadoV6.CostInMoney(MarketRules.SamplePreviewPriceMoney, "Dolar", "carro");
Console.WriteLine($"Carro: custo real em {MarketRules.BaseCurrencySymbol} = {M(custoBase)} | " +
                  $"comprando em $Dolar = {M(custoDolar)} (prêmio de inflação do $Dolar)");
compras.TryBuy(comprador.Id, comprador.Funds, MarketRules.SamplePreviewPriceMoney, "Dolar", "carro");
Console.WriteLine($"Caixa do comprador após o carro: {M(comprador.Funds.Balance)}");

// 14. Câmbio flutuante no tempo (v7): a taxa caminha por um random walk diário.
Console.WriteLine("\n— Câmbio flutuante (v7) —");
var mercadoV7 = new CurrencyMarket(exchangeRates: new ExchangeRateEngine(seed: 2026));
mercadoV7.Currencies.Add(new Currency
{
    Id = "Euro", Name = "$Euro", Symbol = "$E",
    UnitsPerMoney = 4m, ExchangeRateVolatilityPercent = 6m, // ±6%/dia
});
Console.WriteLine($"$Euro: taxa nominal 4.0, volatilidade ±6%/dia. Cotação semanal do carro:");
for (int semana = 0; semana <= 6; semana++)
{
    if (semana > 0)
        for (int d = 0; d < 7; d++) mercadoV7.AdvanceDay();
    Console.WriteLine($"  dia {mercadoV7.Calendar.CurrentDay,3}: taxa efetiva {mercadoV7.EffectiveRate("Euro"):0.###}  " +
                      $"carro = {mercadoV7.Quote(MarketRules.SamplePreviewPriceMoney, "Euro")}");
}
Console.WriteLine($"(índice de câmbio grampeado à banda [{MarketRules.MinRateIndex}, {MarketRules.MaxRateIndex}]× — a taxa não dispara)");
