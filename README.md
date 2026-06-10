# Openworld-design-template

Sistema de relacionamento em **C# / .NET 8**, inspirado em *The Sims 2*
(eixos Daily/Lifetime, atração/chemistry, interesses de conversa, wants &
fears), *The Sims 4* (trilhas separadas de amizade/romance, sentimentos
persistentes), *Crusader Kings 3* (modificadores nomeados e temporários) e em
sistemas de atração assimétrica.

## Conceito

Cada relacionamento é **direcional**: `A → B` é independente de `B → A`,
o que permite paixão não-correspondida e mágoa unilateral.

Cada direção tem **duas trilhas independentes** (estilo The Sims 4, onde
amizade e romance são barras separadas) — `Value`/`Friendship` e `Romance`.
Dá para ter amizade alta sem romance, ou romance sem amizade. Cada trilha
carrega dois eixos de tempo, todos limitados a `[-100, +100]`:

| Eixo         | Prazo      | Decaimento                                   |
|--------------|------------|----------------------------------------------|
| **Daily**    | Curto, volátil | Decai **2 pts/dia** rumo a zero (sem contato) |
| **Lifetime** | Longo, estável | Não decai por tempo; **normaliza +3** rumo ao daily (3×/dia) |

Os flags leem trilhas diferentes:

| Flag           | Trilha    | Regra                              |
|----------------|-----------|------------------------------------|
| **Friend**     | Amizade   | daily mútuo ≥ 50                   |
| **BestFriend** | Amizade   | lifetime mútuo ≥ 50               |
| **Enemy**      | Amizade   | daily ≤ -50                       |
| **Crush**      | Romance   | romance daily ≥ 70 (forma só em contexto romântico) |
| **Love**       | Romance   | romance lifetime ≥ 70 (forma só em contexto romântico) |

A amizade quebra assim que o daily mútuo cai abaixo de 50; o amor quebra
quando o romance lifetime cai abaixo de 70 (inclusive por interações não
românticas, como um insulto, que também ferem o romance).

## Estrutura

```
RelationshipSystem.sln
├── src/RelationshipSystem.Core/         # biblioteca principal
│   ├── CharacterTraits.cs               # Zodiac, Aspiration, Personality, TurnOns/Off, Tags, Interests
│   ├── RelationshipValue.cs             # Daily/Lifetime (uma trilha) + clamp, normalize, decay
│   ├── RelationshipModifier.cs          # modificador nomeado e temporário
│   ├── Sentiment.cs                     # sentimento persistente direcional (TS4)
│   ├── Relationship.cs                  # A→B: trilhas amizade+romance, flags, modifiers, sentiments
│   ├── InteractionDefinition.cs         # Availability, Accepted, Effects
│   ├── RelationshipMatrix.cs            # dicionário (From,To) → Relationship + getters
│   ├── CharacterRegistry.cs             # id → CharacterTraits (para interesses)
│   ├── RelationshipThresholds.cs        # thresholds, physics, pesos de atração/interesses/sentimentos
│   ├── AttractionCalculator.cs          # atração assimétrica (TS2)
│   ├── InterestCalculator.cs            # bônus de conversa por interesse mútuo (TS2)
│   ├── ZodiacCompatibilityTable.cs      # compatibilidade por elementos
│   ├── InteractionResolver.cs           # executa interações, emite eventos
│   ├── RelationshipDecaySystem.cs       # DailyTick / NormalizationTick
│   ├── Desire.cs                        # Want/Fear declarativo (TS2)
│   ├── AspirationMeter.cs               # barra de aspiração [-100,100]
│   ├── WantsAndFearsSystem.cs           # avalia desejos, move aspiração, emite eventos
│   ├── RelationshipDesires.cs           # fábrica de wants & fears comuns
│   └── Interactions/InteractionLibrary.cs
├── tests/RelationshipSystem.Tests/      # xUnit
├── samples/RelationshipSystem.Demo/     # console de exemplo (Parte 7 da spec)
│
├── src/EconomySystem.Core/              # módulo de economia (ver docs/economia-design.md)
│   ├── MoneyTransaction.cs              # lançamento nomeado (~ RelationshipModifier)
│   ├── HouseholdFunds.cs                # caixa do domicílio (~ RelationshipValue)
│   ├── OwnedObject.cs / HouseholdInventory.cs  # bens + depreciação (~ Matrix)
│   ├── Household.cs                     # caixa + inventário + moradores + patrimônio
│   ├── BillsSystem.cs                   # contas, desconto por filho, repo-man
│   ├── IncomeActivityDefinition.cs / IncomeResolver.cs  # renda freelance (~ Interaction)
│   ├── EconomyTickSystem.cs             # tick diário (~ RelationshipDecaySystem)
│   ├── EconomyThresholds.cs             # thresholds/physics/business rules
│   ├── Economy/IncomeActivityLibrary.cs # catálogo (pintura, colheita, artesanato)
│   ├── Businesses/                      # Open for Business: estoque, venda, fidelidade
│   ├── CareerDefinition.cs / CareerState.cs / CareerResolver.cs  # carreiras (v2)
│   ├── Careers/CareerLibrary.cs         # carreiras: tradicional, remoto, gig (v2)
│   ├── Subscription.cs                  # assinatura digital recorrente (v2)
│   ├── Digital/DigitalIncomeLibrary.cs  # bico por app, marketplace (v2)
│   ├── ChanceCard.cs                    # evento de trabalho com 2 opções (v3)
│   ├── Careers/ChanceCardLibrary.cs     # catálogo de chance cards (v3)
│   ├── Businesses/BusinessPerk.cs       # perks de ranking + desconto de atacado (v3)
│   ├── AspirationWallet.cs / AspirationReward.cs / AspirationResolver.cs  # moeda soft (v3)
│   ├── Aspiration/AspirationRewardCatalog.cs  # recompensas (inclui Árvore do Dinheiro) (v3)
│   ├── Integration/PaidInteractionResolver.cs   # presentes pagos (v2)
│   └── Integration/RelationshipEconomyBridge.cs # cliente = relacionamento; gate de amigos
├── tests/EconomySystem.Tests/           # xUnit
├── samples/EconomySystem.Demo/          # console: loop econômico v1–v3 ponta-a-ponta
│
├── src/BattleSystem.Core/               # etapa de batalhas (ver docs/batalha-design.md)
│   ├── CombatantStats.cs                # atributos validados fail-fast (~ CharacterTraits)
│   ├── Combatant.cs                     # vida/stamina com clamp (~ RelationshipValue)
│   ├── StatusEffect.cs                  # status nomeado/temporário em turnos (~ RelationshipModifier)
│   ├── BattleActionDefinition.cs        # ação declarativa (~ InteractionDefinition)
│   ├── Battle.cs                        # times, iniciativa por velocidade, rodadas
│   ├── BattleResolver.cs                # executa ações, falha rápido, emite eventos
│   ├── TurnSystem.cs                    # veneno, envelhecimento de status, regen (~ DecaySystem)
│   ├── BattleThresholds.cs              # thresholds/physics/morale rules
│   ├── Actions/BattleActionLibrary.cs   # catálogo (golpes, defesa, veneno, cura...)
│   ├── Ai/SimpleBattleAI.cs             # IA heurística: torna a batalha funcional sem input
│   └── Integration/RelationshipBattleBridge.cs  # moral pré-batalha; desfecho vira modificador
├── tests/BattleSystem.Tests/            # xUnit
├── samples/BattleSystem.Demo/           # console: batalha 2x2 ponta-a-ponta, sem assets
│
└── godot/                               # adaptador Godot 4 (.NET) — FORA da sln/CI
    ├── project.godot                    # protótipo jogável sem nenhum asset
    ├── scenes/Battle.tscn
    └── scripts/BattleController.cs      # eventos C# do núcleo → [Signal] + UI por código
```

## Módulo de Economia

Camada econômica inspirada em The Sims 2 ("simplicidade amplificada": moeda única,
sem inflação, loop **ganhar → guardar → gastar → o tempo passa**). É um projeto
irmão com dependência **unidirecional** `EconomySystem.Core → RelationshipSystem.Core`,
reusando os mesmos padrões (modificadores nomeados, store por dicionário,
definições declarativas + resolver, tick de tempo, thresholds centralizados).
A integração principal: **clientes do negócio são relacionamentos** — a disposição
de compra deriva do `EffectiveDaily` e uma boa venda devolve um modificador ao
relacionamento. A v2 adiciona **carreiras com promoções gated por amigos** (via
`AreFriends`/`CountFriends`), salários no tick, e uma **camada moderna** (pistas
remoto/gig, assinaturas digitais, renda por app/marketplace, presentes pagos) —
indo além do Ocidente dos anos 2000 que o TS2 retrata. A v3 aprofunda o negócio
próprio (**perks de ranking** + folha de pagamento no tick), traz **chance cards**
de duas opções e a **moeda "soft" de aspiração** (com objetos de recompensa).
Spec completa em [`docs/economia-design.md`](docs/economia-design.md).

## Etapa de Batalhas (e migração Unreal → Godot)

Módulo de combate **por turnos** em `BattleSystem.Core`, projeto irmão com a
mesma regra de dependência unidirecional (`BattleSystem.Core →
RelationshipSystem.Core`) e os mesmos padrões: stats validados fail-fast,
status nomeados/temporários (em turnos), ações declarativas + resolver com
eventos, tick de turno e thresholds centralizados. A integração com
relacionamentos é uma costura única (`RelationshipBattleBridge`): a matriz
vira **moral pré-batalha** (lutar ao lado de amigos anima; enfrentar rivais
enfurece) e o desfecho vira **memória** (modificadores temporários — uma
derrota pode criar uma inimizade real).

O projeto migrou da **Unreal para a Godot Engine**: como toda regra vive em
C#/.NET 8 puro, a mudança custa só a casca — o adaptador em `godot/` (Godot 4
.NET, fora da sln/CI) assina os eventos C# do núcleo e os re-emite como
`[Signal]`, com UI construída em código para ser **jogável sem nenhum asset**
(a `SimpleBattleAI` joga os dois lados). Alinhamento planejado × entregue,
mapeamento dos padrões Unreal→Godot, lista de assets open source (CC0/CC-BY)
e roadmap v2–v3 em [`docs/batalha-design.md`](docs/batalha-design.md).

## Decisões de implementação

- **Fail-fast nas validações.** `CharacterTraits` valida nos setters `init`
  (exatamente 2 turn-ons, turn-off não-vazio, personalidade 0..10). A spec
  original validava num construtor sem parâmetros, o que **não funciona** com
  propriedades `required init` (o corpo do construtor roda antes dos
  inicializadores). Por isso a validação foi movida para os `init`.
- **Modificadores clonados por relacionamento.** `InteractionEffect` carrega um
  *template* de modificador; o resolver clona-o ao aplicar, evitando que
  `RemainingHours` (mutável) seja compartilhado entre relacionamentos.
- **`IsFurious`** é derivado de um modificador de fúria ativo
  (`Relationship.FuryModifierName`), gerado por `Insult`.
- **Score efetivo** ignora modificadores expirados (`EffectiveDaily/Lifetime`).
- A integração com Godot (Parte 8 da spec) ficou fora da sln para manter o
  núcleo livre de dependências; ela agora **existe** em `godot/` (protótipo da
  etapa de batalhas), que reutiliza `RelationshipSystem.Core` e
  `BattleSystem.Core` sem alterações e só compila dentro da engine.

## Sentimentos (The Sims 4)

Cada relacionamento mantém uma camada **narrativa** separada do score: até
`SentimentDefaults.MaxSentiments` (4) sentimentos persistentes e direcionais
(`Close`, `Adoring`, `Enamored`, `Motivated`, `Bitter`, `Hurt`, `Guilty`,
`Resentful`). Ao exceder o limite, o mais fraco é descartado; reaplicar o mesmo
tipo **reforça** (intensidade acumula até o teto, prazo é estendido) em vez de
duplicar. Sentimentos de **longo prazo** não decaem; os de **curto prazo**
envelhecem no `DailyTick`.

Eles **não entram no score efetivo** (são feeling, não pontuação). Surgem
automaticamente dos marcos já emitidos pelo resolver (amizade → `Close`, amor →
`Enamored`, inimizade → `Bitter`, coração partido → `Hurt`) e também podem ser
autorados por interação via `InteractionEffect.ResultingSentiment` (ex.: presente
→ `Adoring`, insulto → `Resentful`).

## Interesses / Tópicos de conversa (The Sims 2)

`CharacterTraits.Interests` mapeia tópicos (strings livres; veja
`InterestTopics`) para um nível `0..10`. Ao executar uma conversa com tópico —
`resolver.Perform(from, to, def, topic)` — o ganho de Daily é modulado pelo
interesse **mútuo**: conversar sobre algo que ambos amam acelera (até +3), sobre
algo que entedia os dois esfria (até -3). Requer um `CharacterRegistry` no
resolver; sem ele, o tópico é ignorado e o comportamento é idêntico ao anterior.

## Wants & Fears (The Sims 2)

Cada personagem tem uma **barra de aspiração** (`AspirationMeter`, `[-100,100]`)
e um conjunto de **desejos** dinâmicos (`Desire`): cumprir um *Want* enche a
aspiração; realizar um *Fear* a drena. Cada desejo é direcional (sobre um alvo) e
sua condição é um predicado sobre a matriz — o que permite medos como "amar sem
ser correspondido" (`RelationshipDesires.UnrequitedLoveFear`), que olha as duas
direções.

O `WantsAndFearsSystem` guarda as barras e os desejos por personagem, reavalia
com `Evaluate(matrix, ownerId)` (desejos realizados são *one-shot*: ajustam a
aspiração, emitem `WantFulfilled`/`FearRealized` e somem) e pode se auto-ligar a
um resolver com `AttachTo(resolver, matrix)` — reavaliando os dois envolvidos
após cada interação. `RelationshipDesires` traz atalhos comuns (ficar amigo,
apaixonar-se, virar inimigo, amor não-correspondido).

## Build & testes

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project samples/RelationshipSystem.Demo
dotnet run --project samples/EconomySystem.Demo   # loop econômico v1–v3 ponta-a-ponta
dotnet run --project samples/BattleSystem.Demo    # batalha 2x2 ponta-a-ponta (sem assets)
```

Requer o SDK do .NET 8.
