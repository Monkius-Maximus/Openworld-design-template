using EconomySystem.Core;
using EconomySystem.Core.Aspiration;
using EconomySystem.Core.Businesses;
using EconomySystem.Core.Careers;
using EconomySystem.Core.Digital;
using EconomySystem.Core.Economy;
using EconomySystem.Core.Integration;
using RelationshipSystem.Core;

// ─────────────────────────────────────────────────────────────────────────────
// Demo da economia inspirada em The Sims 2 ("simplicidade amplificada").
// Mostra o loop fechado: ganhar → guardar → gastar → o tempo passa,
// e a integração com o núcleo de relacionamentos (v1–v3).
// ─────────────────────────────────────────────────────────────────────────────

static string M(int simoleons) => $"§{simoleons}";

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
Console.WriteLine($"Reposição 6×§50 com perk ({loja.RestockDiscountPercent}% off): {M(caixaAntes)} → {M(lar.Funds.Balance)}");

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

// 9. Presente pago (v2): custa Simoleons e afeta o relacionamento.
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
