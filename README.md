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
└── samples/RelationshipSystem.Demo/     # console de exemplo (Parte 7 da spec)
```

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
- A integração com Godot (Parte 8 da spec) foi omitida do build para manter o
  núcleo livre de dependências; o `RelationshipManager` pode ser adicionado num
  projeto Godot reutilizando `RelationshipSystem.Core` sem alterações.

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
```

Requer o SDK do .NET 8.
