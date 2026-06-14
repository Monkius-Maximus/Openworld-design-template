# Mercado Financeiro Simulado — design (v4 + v5 + v6)

Este documento especifica a camada de **mercado financeiro** do projeto
(`EconomySystem.Core/Market/`): moeda base nomeada, inflação global e local,
registro de moedas com câmbio, persistência e UI de gerenciamento no Godot
(v4); **eventos econômicos** (choques) que fecham o loop com o tick clássico
via **renda indexada** (v5 — seção 8); e **gasto em moeda estrangeira**,
**eventos aleatórios** e **histórico/gráfico** (v6 — seção 9).

> Filosofia: o núcleo econômico mantém a **simplicidade amplificada** de
> [economia-design.md](economia-design.md) — o caixa do domicílio continua
> mono-moeda, em inteiros. O mercado é uma camada **opt-in** por cima: nada no
> tick clássico (`DailyTick(Household, DayOfWeek)`) muda sem ele. É o estilo
> tycoon: o jogador (ou o designer) liga o mercado quando quer profundidade
> macroeconômica, sem pagar o custo dela no loop básico.

## 1. Pesquisa de referência

- **Jogos tycoon (Transport Tycoon/OpenTTD, Capitalism II)**: inflação como
  **deriva composta lenta** sobre preços de catálogo, não como choque — o
  jogador percebe a tendência olhando o histórico, não um salto anual.
- **Animal Crossing (stalk market) / EVE Online**: mercados como camada
  opcional de risco/profundidade por cima de uma moeda estável e legível.
- **The Sims 2**: referência do núcleo — preços fixos e "errados" de propósito.
  A v4 não quebra isso: o preço **de catálogo** segue fixo em $Money; índices
  multiplicativos entram só na **cotação**.

## 2. Conceitos

| Conceito | Classe | Resumo |
|----------|--------|--------|
| Moeda base | `Currency.CreateBase()` | **$Money** (símbolo `$M`), taxa 1:1, nunca removível. É a unidade canônica de `HouseholdFunds`/`MoneyTransaction`. |
| Moeda derivada | `Currency` | Nome, símbolo, `UnitsPerMoney` (1 $Money = N unidades), inflação anual projetada, dia de criação. Ex.: $Dolar, $Real, $Euro. |
| Registro | `CurrencyRegistry` | Store por Id (case-insensitive), semeia a base, eventos `CurrencyAdded`/`CurrencyRemoved`. |
| Calendário | `SimulationCalendar` | Dia corrido → ano (`DaysPerYear = 364` = 52 semanas, alinhado ao ciclo de contas terça/quinta) e dia da semana. |
| Inflação | `InflationEngine` | Índices compostos diariamente: global, por produto e por moeda. |
| Cotação | `CurrencyMarket.Quote` → `PriceQuote` | Pipeline de preço (abaixo). |
| Persistência | `MarketStateSerializer` | Round-trip JSON versionado, sem IO (a engine é dona do arquivo). |

## 3. Pipeline de preço

```
preço final na moeda C =
  Round( base($Money)
       × GlobalIndex            ← inflação global: todos os produtos, todas as moedas
       × ProductIndex(produto)  ← inflação local de UM produto (opcional)
       × C.UnitsPerMoney        ← câmbio
       × CurrencyIndex(C) )     ← inflação local de UMA moeda
```

- Arredondamento `AwayFromZero` **só no passo final** — fatores intermediários
  ficam em `decimal` para não compor erro de arredondamento.
- Inflação local numa **moeda** encarece tudo que se cota nela (a moeda se
  desvaloriza) sem tocar no preço em $Money — exatamente a semântica pedida:
  "se uma currency é afetada, todos os produtos compráveis com ela também são".
- A própria $Money pode receber inflação local (é "uma moeda específica");
  basta semear a base com `ProjectedAnnualInflationPercent != 0`.

## 4. Como a inflação compõe

- **Cadência diária, geométrica**: `fator_diário = (1 + taxa_anual/100)^(1/364)`.
  Após 364 ticks, o índice rende exatamente o fator anual (deriva suave em vez
  de choque de preço por ano).
- **Global**: sempre ativa (`InflationEngine.GlobalAnnualPercent`).
- **Por produto**: ativa imediatamente ao chamar `SetProductInflation`.
- **Por moeda**: a taxa é declarada na criação (`ProjectedAnnualInflationPercent`),
  mas **só compõe a partir de `CreatedOnDay + 364`** (1 ano de simulação) —
  `Currency.InflationActivationDay`. Até lá o índice fica exatamente em `1m`.
- Limites de sanidade em `MarketRules`: taxa anual em [−50%, 1000%], câmbio
  ≥ 0.0001.

## 5. Tick

`CurrencyMarket.AdvanceDay()` avança o calendário e compõe a inflação do dia.
O `EconomyTickSystem` ganhou um overload dirigido pelo mercado:

```csharp
ticks.DailyTick(domicilios, market);
// = market.AdvanceDay() + DailyTick(domicilios, market.Calendar.DayOfWeek,
//                                   market.Calendar.CurrentDay)
```

— o calendário vira o produtor real do `gameDay` das transações, antes um
inteiro fornecido pelo chamador.

## 6. Persistência

`MarketStateSerializer.ToJson/FromJson` — schema versionado (`version: 1`):

```json
{
  "version": 1,
  "currentDay": 412,
  "globalAnnualPercent": 3.0,
  "globalIndex": 1.0341,
  "currencies": [
    { "id": "Money", "name": "$Money", "symbol": "$M", "unitsPerMoney": 1.0,
      "projectedAnnualInflationPercent": 0.0, "createdOnDay": 0, "isBase": true },
    { "id": "Dolar", "name": "$Dolar", "symbol": "$D", "unitsPerMoney": 5.2,
      "projectedAnnualInflationPercent": 4.5, "createdOnDay": 30, "isBase": false }
  ],
  "products": { "carro": { "annualPercent": 8.0, "index": 1.0123 } },
  "currencyIndices": { "Dolar": 1.0021 }
}
```

Os **índices acumulados** são persistidos (não só as taxas) para que carregar
um save no meio do ano não zere a deriva já composta. O serializer é puro
string↔objeto; quem grava o arquivo é a camada Godot
(`user://market_state.json`).

## 7. UI Godot (gerenciador de moedas)

- **`MarketController`** (`godot/src/MarketController.cs`): dono do
  `CurrencyMarket`; carrega o save no `_Ready`, salva ao adicionar/remover e no
  fechamento da janela; a ação `market_toggle` (**tecla M**) mostra/esconde o
  painel.
- **`CurrencyManagerPanel`** (`godot/src/CurrencyManagerPanel.cs`): segue o
  padrão do `BuildController` ([Export] validados em `_Ready`, callbacks via
  `Callable.From`). Campos: **Nome**, **Símbolo**, **Taxa** (1 $M = N) e
  **Inflação projetada %/ano**. O **preview ao vivo** cota o produto-exemplo
  (carro = `MarketRules.SamplePreviewPriceMoney` = 10.000 $Money) a cada tecla:

  ```
  Carro: $M10000 → $D52000
  Inflação projetada 4,5%/ano (ativa a partir do dia 394; hoje é o dia 30)
  ```

  A lista de moedas registradas mostra a cotação ao vivo do carro em cada
  moeda e um botão **Remover** (desabilitado para a base). Validação é a do
  core (fail-fast): entradas inválidas viram mensagem no próprio preview.

## 8. Eventos econômicos e renda indexada (v5)

A v5 fecha o loop entre o mercado e o tick clássico: choques macroeconômicos
distorcem a inflação **e** o salário real dos domicílios.

### 8.1 Evento econômico

`EconomicEvent` é imutável (validação fail-fast nos `init`, como `Currency`):
uma janela `[StartDay, StartDay + DurationDays)` que, enquanto ativa, carrega
dois efeitos independentes:

| Campo | Efeito |
|-------|--------|
| `GlobalInflationDelta` | Pontos percentuais **somados** à inflação global anual (pode ser negativo: pressão deflacionária). |
| `IncomeMultiplier` | Fator **multiplicativo** (> 0) sobre a renda dos domicílios. `1.2` = boom; `0.85` = recessão. |

`EconomicEventScheduler` guarda os eventos e agrega, para um dado dia, a
**soma** das deltas de inflação e o **produto** dos multiplicadores de renda
dos ativos (janelas podem se sobrepor). `EconomicEventLibrary` traz presets
tunáveis (`Recession`, `Boom`, `Crisis`) no estilo das outras `*Library`.

### 8.2 Pipeline (mudanças mínimas)

- `CurrencyMarket.AdvanceDay` passa `Events.GlobalInflationDeltaOn(hoje)` para
  `InflationEngine.AdvanceDay` como delta extra. A taxa efetiva é **grampeada**
  a `[Min, Max]AnnualInflationPercent` para o fator diário nunca virar inválido
  (base negativa em `Math.Pow`).
- `CurrencyMarket.IncomeAdjustmentFactor()` = `GlobalIndex` (custo de vida
  acumulado) × `Events.IncomeMultiplierOn(hoje)`. É o fator de reajuste do dia.

### 8.3 Renda indexada

`CareerResolver.PayDailyWage` ganha um parâmetro `incomeFactor` (default `1m` =
comportamento clássico). O `EconomyTickSystem.DailyTick(households, market)`
calcula o fator via `IncomeAdjustmentFactor()` e o propaga aos salários — em
tempos normais a renda **real** fica constante (acompanha a inflação); em
boom/recessão ela oscila. Salário reajustado = `round(DailyWage × incomeFactor,
AwayFromZero)`.

### 8.4 Persistência e UI

- O serializer sobe para `version = 2`, gravando os eventos agendados; saves
  `version = 1` (sem eventos) ainda carregam (agenda vazia).
- O `CurrencyManagerPanel` ganha uma seção "Eventos econômicos" (construída em
  código, sem cena/`[Export]` novos): botões de preset que agendam começando
  hoje, o fator de reajuste vigente e a lista de eventos ativos/futuros com
  remoção.

### 8.5 Fora de escopo (ganchos futuros)

- **Gastar em moeda estrangeira** (converter → debitar do caixa $Money): o
  ledger continua mono-moeda; um helper `ConvertAndWithdraw` é o gancho natural.
- **Taxas de câmbio flutuantes** (mercado de moedas como mini-jogo): a deriva
  hoje vem só da inflação; um `ExchangeRateNoise` opcional poderia compor por
  cima do `UnitsPerMoney`.
- **Eventos aleatórios/encadeados**: hoje os eventos são agendados
  explicitamente; um gerador estocástico (ou chance cards globais) poderia
  alimentar o `EconomicEventScheduler`.

## 9. Câmbio em uso, eventos aleatórios e histórico (v6)

A v6 dá **uso** às moedas derivadas, automatiza os choques e torna a deriva
observável.

### 9.1 Gastar em moeda estrangeira

O ledger continua mono-moeda. `CurrencyMarket.CostInMoney(basePrice,
currencyId, productId?)` cota o item na moeda (o inteiro que o jogador vê) e
**converte de volta** pelo câmbio: para a base é o próprio preço efetivo; para
uma moeda com inflação própria ativa, embute o **prêmio de inflação** dela
(gastar numa moeda inflacionada custa mais $Money de verdade).
`MarketPurchaseResolver.TryBuy(...)` (em `Integration/`) debita esse equivalente
do `HouseholdFunds` via `TryWithdraw`, devolve `false` sem mexer no saldo se
faltar fundo, trata item grátis (custo 0) e dispara `Purchased`.

### 9.2 Eventos aleatórios

`RandomEventGenerator(seed, dailyChance)` alimenta o `EconomicEventScheduler`:
a cada dia, com probabilidade `dailyChance`, sorteia um preset
(recessão/boom/crise) começando naquele dia — **sem empilhar** se já houver
evento ativo. É **determinístico por semente** (mesma seed → mesma sequência),
o que o torna testável; o caller (`MarketController`/demo) chama
`MaybeGenerate` 1×/dia após o `AdvanceDay`.

### 9.3 Histórico e gráfico

`MarketHistory` é um buffer circular de `MarketSample` (dia, índice global,
fator de renda). `CurrencyMarket` aceita um histórico **opcional** no construtor
e grava uma amostra por `AdvanceDay` se ele estiver presente (opt-in: o core
fica leve, e os testes/usos sem histórico não mudam). É **observacional** —
não é serializado; o `MarketStateSerializer.FromJson` aceita um histórico para
anexar ao mercado reconstruído. Na UI, `MarketHistoryChart` (um `Control` que
desenha em código) plota o índice global ao longo do tempo.

### 9.4 UI

O `CurrencyManagerPanel` ganha (tudo em código, sem cena/`[Export]` novos):
uma seção "Histórico de inflação" com o gráfico e botões "Avançar 30 dias / 1
ano" (que avançam a simulação, gravam histórico e dão a vez ao gerador
estocástico), e cada linha de moeda passa a mostrar o **custo real em $Money**
ao lado da cotação.

## 10. Testes

`tests/EconomySystem.Tests/`: `SimulationCalendarTests`, `InflationEngineTests`
(composição anual exata, ativação após 1 ano, independência dos índices),
`CurrencyMarketQuoteTests` (exemplo do carro, arredondamento),
`CurrencyRegistryTests`, `CurrencyMarketTests`, `MarketStateSerializerTests`
(round-trip preserva índices e eventos; continuar a simulação após o load é
idêntico a nunca ter salvo; v1 antigo ainda carrega). **v5**:
`EconomicEventTests` (janela meio-aberta, validação, presets),
`EconomicEventSchedulerTests` (soma de deltas, produto de multiplicadores,
sobreposição), `CurrencyMarketEventTests` (delta só compõe na janela; clamp de
delta extrema), `IndexedIncomeTests` (salário escala pelo fator; tick dirigido
pelo mercado indexa à inflação; recessão corta a renda real). **v6**:
`MarketSpendTests` (custo na base = preço efetivo; prêmio de inflação da moeda;
`TryBuy` debita/falha/grátis), `RandomEventGeneratorTests` (determinismo por
semente, chance 0/limites, não empilha), `MarketHistoryTests` (buffer circular;
mercado grava 1 amostra/dia quando anexado). Demo: seções "Mercado financeiro
(v4)", "Eventos econômicos e renda indexada (v5)" e "Gastar em moeda, eventos
aleatórios e histórico (v6)" em `samples/EconomySystem.Demo`.
