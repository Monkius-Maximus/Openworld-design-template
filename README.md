# Openworld-design-template

Sistema de relacionamento em **C# / .NET 8**, inspirado em *The Sims 2*
(eixos Daily/Lifetime), *Crusader Kings 3* (modificadores nomeados e
temporários) e em sistemas de atração assimétrica.

## Conceito

Cada relacionamento é **direcional**: `A → B` é independente de `B → A`,
o que permite paixão não-correspondida e mágoa unilateral. Cada direção
carrega dois eixos, ambos limitados a `[-100, +100]`:

| Eixo         | Prazo      | Decaimento                                   | Rege                              |
|--------------|------------|----------------------------------------------|-----------------------------------|
| **Daily**    | Curto, volátil | Decai **2 pts/dia** rumo a zero (sem contato) | Amizade (≥50 mútuo), inimizade (≤-50) |
| **Lifetime** | Longo, estável | Não decai por tempo; **normaliza +3** rumo ao daily (3×/dia) | Melhor amizade (≥50 mútuo), amor (≥70) |

A amizade quebra assim que o daily mútuo cai abaixo de 50.

## Estrutura

```
RelationshipSystem.sln
├── src/RelationshipSystem.Core/         # biblioteca principal
│   ├── CharacterTraits.cs               # Zodiac, Aspiration, Personality, TurnOns/Off, Tags
│   ├── RelationshipValue.cs             # Daily/Lifetime + clamp, normalize, decay
│   ├── RelationshipModifier.cs          # modificador nomeado e temporário
│   ├── Relationship.cs                  # A→B: flags, modifiers, score efetivo
│   ├── InteractionDefinition.cs         # Availability, Accepted, Effects
│   ├── RelationshipMatrix.cs            # dicionário (From,To) → Relationship + getters
│   ├── RelationshipThresholds.cs        # thresholds, physics e pesos de atração
│   ├── AttractionCalculator.cs          # atração assimétrica (TS2)
│   ├── ZodiacCompatibilityTable.cs      # compatibilidade por elementos
│   ├── InteractionResolver.cs           # executa interações, emite eventos
│   ├── RelationshipDecaySystem.cs       # DailyTick / NormalizationTick
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
│   └── Integration/RelationshipEconomyBridge.cs # cliente = relacionamento
└── tests/EconomySystem.Tests/           # xUnit
```

## Módulo de Economia

Camada econômica inspirada em The Sims 2 ("simplicidade amplificada": moeda única,
sem inflação, loop **ganhar → guardar → gastar → o tempo passa**). É um projeto
irmão com dependência **unidirecional** `EconomySystem.Core → RelationshipSystem.Core`,
reusando os mesmos padrões (modificadores nomeados, store por dicionário,
definições declarativas + resolver, tick de tempo, thresholds centralizados).
A integração principal: **clientes do negócio são relacionamentos** — a disposição
de compra deriva do `EffectiveDaily` e uma boa venda devolve um modificador ao
relacionamento. Carreiras/promoções (gated por amigos via `AreFriends`) e a camada
digital moderna (assinaturas, gig, marketplace) estão desenhadas como fases
posteriores. Spec completa em [`docs/economia-design.md`](docs/economia-design.md).

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

## Build & testes

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project samples/RelationshipSystem.Demo
```

Requer o SDK do .NET 8.
