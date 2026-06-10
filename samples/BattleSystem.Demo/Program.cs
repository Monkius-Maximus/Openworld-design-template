using BattleSystem.Core;
using BattleSystem.Core.Actions;
using BattleSystem.Core.Ai;
using BattleSystem.Core.Integration;
using RelationshipSystem.Core;

// =====================================================================
// Demo da etapa de batalhas — ponta a ponta e SEM NENHUM ASSET:
// 1. a matriz de relacionamentos define amizades e uma rivalidade;
// 2. a ponte converte isso em moral pré-batalha;
// 3. a IA joga os dois lados até o fim;
// 4. o desfecho volta para a matriz como modificadores temporários.
// É exatamente o mesmo núcleo que o adaptador Godot (pasta godot/) usa.
// =====================================================================

Console.WriteLine("=== Etapa de batalhas: demo v1 (núcleo agnóstico de engine) ===\n");

// --- 1. Relacionamentos pré-existentes -------------------------------
var matrix = new RelationshipMatrix();

// Aria e Bruno são amigos de longa data...
matrix.Get("Aria", "Bruno").Value.ApplyDaily(65f);
matrix.Get("Bruno", "Aria").Value.ApplyDaily(65f);

// ...e Aria tem uma rivalidade declarada com Caio.
matrix.Get("Aria", "Caio").Value.ApplyDaily(-70f);

Console.WriteLine("Relacionamentos: Aria & Bruno são amigos; Aria odeia Caio.\n");

// --- 2. Combatentes e batalha ----------------------------------------
Combatant Make(string id, float health, float stamina, float attack, float defense, float speed) =>
    new(new CombatantStats
    {
        Id = id, MaxHealth = health, MaxStamina = stamina,
        Attack = attack, Defense = defense, Speed = speed,
    });

var aria = Make("Aria", health: 90f, stamina: 60f, attack: 12f, defense: 5f, speed: 9f);
var bruno = Make("Bruno", health: 110f, stamina: 50f, attack: 9f, defense: 7f, speed: 5f);
var caio = Make("Caio", health: 100f, stamina: 55f, attack: 11f, defense: 6f, speed: 7f);
var duda = Make("Duda", health: 85f, stamina: 65f, attack: 10f, defense: 4f, speed: 6f);

var battle = new Battle("praca-central", new[] { aria, bruno }, new[] { caio, duda });
var resolver = new BattleResolver(battle, new Random(2026)); // seed p/ demo reprodutível
var turns = new TurnSystem(resolver);
var bridge = new RelationshipBattleBridge(matrix);

// --- 3. Moral pré-batalha vinda da matriz ----------------------------
bridge.ApplyPreBattleMorale(battle);

foreach (var c in battle.Initiative)
foreach (var s in c.Statuses)
    Console.WriteLine($"  [moral] {c.Id}: {s.Name} (atk {s.AttackBonus:+0;-0}, def {s.DefenseBonus:+0;-0})");
Console.WriteLine();

// --- 4. Log de eventos (no Godot, virariam sinais/HUD) ---------------
resolver.ActionPerformed += (actor, target, def, hit) =>
{
    string alvo = actor == target ? "si mesmo" : target.Id;
    Console.WriteLine($"  {actor.Id} usa {def.DisplayName} em {alvo}: {(hit ? "acertou" : "ERROU")}");
};
resolver.DamageDealt += (_, target, amount) =>
    Console.WriteLine($"      → {target.Id} sofre {amount:0.#} de dano (vida: {target.Health:0.#})");
resolver.CombatantDefeated += c =>
    Console.WriteLine($"      ✖ {c.Id} foi derrotado!");
turns.CombatantDefeated += c =>
    Console.WriteLine($"      ✖ {c.Id} sucumbiu aos ferimentos!");
resolver.BattleEnded += (b, winner) =>
    Console.WriteLine($"\n=== Fim da batalha na rodada {b.Round}: vence o time {winner}! ===");

// --- 5. IA joga os dois lados ----------------------------------------
var ai = new SimpleBattleAI();
var pool = BattleActionLibrary.All.Values.ToList();
int round = 0;

while (!battle.IsOver)
{
    if (battle.Round != round)
    {
        round = battle.Round;
        Console.WriteLine($"\n--- Rodada {round} ---");
    }

    var actor = battle.Active;
    var (action, target) = ai.Choose(battle, actor, pool);
    resolver.Perform(actor.Id, action, target.Id);
    if (!battle.IsOver)
        turns.EndTurn();
}

// --- 6. Desfecho escrito de volta na matriz --------------------------
bridge.ApplyPostBattleOutcome(battle);

Console.WriteLine("\nMemórias deixadas na matriz de relacionamentos:");
foreach (var rel in matrix.All)
foreach (var mod in rel.Modifiers.Where(m => !m.IsExpired
             && (m.Name == MoraleRules.DefeatModifierName || m.Name == MoraleRules.ComradeModifierName)))
{
    Console.WriteLine($"  {rel.FromId} → {rel.ToId}: \"{mod.Name}\" ({mod.Value:+0;-0}, expira em {mod.RemainingHours:0}h)");
}

Console.WriteLine("\nDemo concluído. O mesmo loop roda no adaptador Godot (pasta godot/).");
