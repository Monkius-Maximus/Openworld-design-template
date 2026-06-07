# Traços ↔ Carreira — design

> Como os **traços pessoais** de um personagem passam a afetar a **vida
> profissional** (e, no horizonte, a vida em geral), reaproveitando a economia já
> implementada na `main` e o núcleo de relacionamentos — **sem violar a regra de
> ouro** do projeto: dependência **unidirecional** `EconomySystem.Core →
> RelationshipSystem.Core`, com o núcleo de relações intocado e toda a costura
> vivendo na camada `Integration/`.

Escopo escolhido para esta rodada: **TraitProfile → carreira**. Reputação e
Bem-estar entram como pilares **adjacentes**, apenas esboçados, com os ganchos já
preparados (o "−saúde" do workaholic precisa de um sink — ver §7).

---

## 1. Pesquisa de referência

### The Sims 2 (base do projeto)
- Promoção = **gate determinístico**: habilidades + nº de **amigos** + **humor**
  do próximo nível. É exatamente o que `CareerState.EligibleForPromotion` faz hoje.
- Personalidade contínua (Neat/Outgoing/Active/Playful/Nice, 0..10) + **aspiração**
  (Family/Wealth/Popularity/Knowledge/Romance/Pleasure).
- A aleatoriedade da vida profissional aparece nas **chance cards** (eventos de
  trabalho com desfechos) — já previstas como v3 da economia.

### The Sims 3 (traços discretos afetando carreira)
- **Ambitious**: ganha desempenho de trabalho mais rápido → **promoções e
  aumentos mais rápidos**; +15% de felicidade ao cumprir desejos; **custo**:
  moodlet negativo *"Anxious to Advance"* se ficar tempo demais sem progredir.
- **Workaholic**: trabalhar o deixa **menos estressado** (moodlet positivo no
  trabalho) e ele pode trabalhar de casa para subir a barra de desempenho;
  **custo**: sofre quando fica **longe do trabalho** tempo demais.
- **Schmoozer**: melhor socialmente → ajuda o **gate de amigos** das promoções.

**Síntese para o nosso exemplo** ("workaholic → +promoção, −saúde"): no TS3 isso é
a fusão de *Ambitious* (odds de promoção) com um **custo de bem-estar**. Vamos
modelar um perfil próprio que captura essa intenção, **derivado do que já existe**
(Personality/Aspiration) e, opcionalmente, de um traço discreto novo.

Fontes: [Ambitious (TS3) — Wiki](https://sims.fandom.com/wiki/Ambitious_(The_Sims_3)),
[Workaholic (TS3) — Wiki](https://sims.fandom.com/wiki/Workaholic),
[Carl's: Good Career Traits](https://www.carls-sims-3-guide.com/careers/good-traits.php),
[Careers (StrategyWiki, TS2)](https://strategywiki.org/wiki/The_Sims_2/Careers).

---

## 2. Estado atual e pontos de costura

O `CareerContext` é `(Skills, Mood, FriendCount)` e a promoção é um **gate
booleano** (`CareerState.EligibleForPromotion`). Hoje:

- `RelationshipEconomyBridge.EvaluatePromotion` injeta o `FriendCount` (lendo o
  `RelationshipMatrix.AreFriends`) — **o único traço social já conectado**.
- **`Mood` e `Skills` são entradas soltas**: nenhum subsistema os produz; o caller
  os fornece. (Fértil: o pilar de Bem-estar, §7, será o produtor natural do humor.)
- Meu branch trouxe o **`CharacterRegistry` (id → `CharacterTraits`)** — a peça que
  faltava para a ponte **ler traços por id**.

**Onde plugar, sem editar o core da economia:**
`CareerState.EligibleForPromotion(ctx)` e `CareerResolver.TryPromote(state, ctx)`
são públicos. Dá para inserir uma **camada probabilística** inteiramente na ponte:
checar a elegibilidade (gate do TS2) e, se elegível, **rolar** a promoção com odds
moduladas por traço, só então chamar `TryPromote`. Nada no `EconomySystem.Core`
(fora de `Integration/`) precisa mudar.

---

## 3. Modelo de dados proposto

### 3.1 Traços discretos (núcleo de relações — aditivo)

`CharacterTraits` ganha uma lista **opcional** (default vazio, retrocompatível,
validada no `init` como as demais):

```csharp
public enum PersonalityTrait
{
    Ambitious, Workaholic, Lazy, Genius, Charismatic, Family, // ...
}

// em CharacterTraits:
public IReadOnlyList<PersonalityTrait> Traits { get; init; } = Array.Empty<...>();
```

> Alternativa considerada: **derivar tudo** de `Personality`+`Aspiration` (zero
> dado novo). É mais barato, mas perde a legibilidade discreta do TS3 ("é
> workaholic" é mais claro que "Active≥8 e Aspiration=Wealth"). Recomendação:
> **traços discretos** + um *fallback* derivado, para quem não setar traços.

### 3.2 `TraitProfile` (resumo dos efeitos)

Um `readonly record struct` que condensa os traços (discretos + derivados) nos
**efeitos** que importam para a economia — para a ponte não reimplementar regras:

```csharp
public readonly record struct TraitProfile(
    double PromotionOddsBonus,   // somado à base de promoção
    double WageModifier,         // multiplicador de salário (1.0 = neutro)
    int    WellbeingDrainPerDay  // custo de bem-estar por dia de trabalho
);
```

### 3.3 `CareerTraitCalculator` (puro — camada de integração)

Espelha o `AttractionCalculator`: função pura, fácil de testar, **sem estado**.

```csharp
public static class CareerTraitCalculator
{
    public static TraitProfile Profile(CharacterTraits c);          // traços -> efeitos
    public static double PromotionOdds(TraitProfile p, double @base);
}
```

---

## 4. Fórmulas (fase T1)

A promoção passa de **gate puro** para **gate + rolagem** (a camada chance-card):

1. **Pré-condição dura** (inalterada): `EligibleForPromotion` — skills + amigos +
   humor do próximo nível. Sem isso, **não rola**.
2. **Rolagem** quando elegível:
   `odds = clamp(BasePromotionChance + Σ bônus de traço, 0.05, 0.95)`.

Valores iniciais (tunáveis num `CareerTraitWeights` estático, no estilo
`AttractionWeights`):

| Traço        | PromotionOddsBonus | WageModifier | WellbeingDrain/dia |
|--------------|--------------------|--------------|--------------------|
| Ambitious    | +0.25              | ×1.00        | +1 (ansiedade)     |
| Workaholic   | +0.15              | ×1.00        | **+3** (esgota)    |
| Lazy         | −0.20              | ×1.00        | −1 (descansa)      |
| Charismatic  | +0.05              | ×1.05        | 0                  |

- `BasePromotionChance` = 0.5 (em `EconomyThresholds`/novo `CareerTraitWeights`).
- **Salário**: aplicado como multiplicador no valor pago (a ponte pode ajustar o
  `MoneyTransaction` de salário). Mantido neutro para a maioria na T1.
- **Custo (workaholic → −saúde)**: `WellbeingDrainPerDay` é a saída que o **pilar
  de Bem-estar** (§7) vai consumir. Na T1, sem o pilar, o dreno é **emitido como
  evento** (`WellbeingDrained`) e/ou debitado em pontos de aspiração — um
  *placeholder* honesto, sem fingir que a saúde já existe.

---

## 5. Integração — onde o código mora

Tudo novo fica em `EconomySystem.Core/Integration/` (ou núcleo de relações, no
caso dos dados de traço), preservando a direção da dependência:

- **`RelationshipSystem.Core`**: `PersonalityTrait`, `CharacterTraits.Traits`
  (aditivo). É dado de personagem — pertence ao núcleo, e a economia já o lê.
- **`EconomySystem.Core/Integration/CareerTraitCalculator.cs`**: puro.
- **`RelationshipEconomyBridge`** ganha um overload que recebe o
  `CharacterRegistry` e faz a rolagem:

```csharp
public bool EvaluatePromotion(
    CareerResolver resolver, CareerState state,
    IReadOnlyDictionary<string,int> skills, int mood,
    CharacterRegistry characters)
{
    var ctx = new CareerContext(skills, mood, CountFriends(state.CharacterId));
    if (!state.EligibleForPromotion(ctx)) return false;        // gate TS2

    var profile = characters.TryGet(state.CharacterId, out var c)
        ? CareerTraitCalculator.Profile(c) : default;
    double odds = CareerTraitCalculator.PromotionOdds(profile, BasePromotionChance);

    return _roll() < odds && resolver.TryPromote(state, ctx);   // camada chance-card
}
```

> **Recomendação de testabilidade**: tornar a fonte de aleatoriedade **injetável**
> (`Func<double> roll`, default `Random.Shared.NextDouble`) — hoje a ponte usa
> `Random.Shared` direto, o que dificulta testar a rolagem de forma determinística.

O overload **antigo** de `EvaluatePromotion` continua existindo (sem traços =
gate puro), então nada do que já está na `main` quebra.

---

## 6. Arquitetura — espelhando os dois núcleos

| Peça (esta integração)       | Análogo existente             | Padrão reusado                         |
|------------------------------|-------------------------------|----------------------------------------|
| `PersonalityTrait` / `Traits`| `Aspiration` / `TurnOns`      | enum + lista opcional validada no `init` |
| `TraitProfile`               | `CareerContext`               | `readonly record struct` de entrada    |
| `CareerTraitCalculator`      | `AttractionCalculator`        | calculadora **pura**, estática         |
| `CareerTraitWeights`         | `AttractionWeights`           | um arquivo, `static` por preocupação   |
| rolagem na ponte             | `chance cards` (economia v3)  | desfecho probabilístico sobre o gate   |
| `WellbeingDrained` (evento)  | delegates do `InteractionResolver` | evento por delegate              |

Regras herdadas, reafirmadas: **dependência unidirecional** `Economy →
Relationship`; **núcleo de relações livre de dependências**; costura só em
`Integration/`; **fail-fast** nos `init`; catálogos **declarativos**.

---

## 7. Fases (e os pilares adjacentes)

| Fase | Conteúdo | Depende de |
|------|----------|-----------|
| **T1** (foco) | `PersonalityTrait`+`Traits`, `CareerTraitCalculator`, rolagem de promoção por traço na ponte, salário modulado, evento de dreno | `CharacterRegistry` (já no branch) |
| **T2** | **Pilar Bem-estar** (saúde física/mental, [0..100]): consome o `WellbeingDrain`, **produz o `Mood`** hoje solto, e realimenta as interações sociais (bem-estar baixo piora conversas) | T1 |
| **T3** | **Reputação** pública por personagem: afeta promoção (economia) e como **estranhos** tratam o personagem (relações). Mora no núcleo de character p/ manter a direção da dependência | — |
| **T+** | Traços também alimentam `AttractionCalculator`/interações (workaholic é menos disponível socialmente etc.) | núcleo |

O **Bem-estar (T2) é o sink natural** do custo do workaholic: é ele que fecha o
laço "trabalha demais → saúde cai → humor cai → promoção fica mais difícil",
exatamente o trade-off que você descreveu. Por isso a T1 já **emite** o dreno,
mesmo antes do pilar existir.

---

## 8. Plano de testes (quando implementar)

- **`CareerTraitCalculator` (puro)**: perfil por traço; soma de bônus; `clamp` das
  odds; multiplicador de salário; dreno do workaholic > 0.
- **Bridge (rolagem determinística via `roll` injetado)**: elegível + traço alto +
  `roll` baixo → promove; elegível + `roll` alto → **não** promove; **não** elegível
  → nunca rola (gate do TS2 mantido); overload antigo inalterado.
- **Retrocompat**: personagem sem `Traits` → `TraitProfile` neutro → comportamento
  idêntico ao da `main`.

---

## 9. Decisões em aberto

1. **Traços discretos** (recomendado) **vs.** derivar de `Personality/Aspiration`.
2. **Aleatoriedade injetável** na ponte (recomendado) — pré-requisito para testar
   a rolagem sem flakiness.
3. **Convergência de branches**: esta integração precisa do núcleo enriquecido
   (CharacterRegistry) **e** da economia juntos. O merge é quase limpo (só o
   `README.md` conflita). Definir se a base será a `main` (após merge) ou o branch
   de feature (após trazer a economia) **antes** de codar a T1.
