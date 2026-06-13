# Mercado Financeiro Simulado — design (v4)

Este documento especifica a camada de **mercado financeiro** do projeto
(`EconomySystem.Core/Market/`): moeda base nomeada, inflação global e local,
registro de moedas com câmbio, persistência e UI de gerenciamento no Godot.

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

## 8. Fora de escopo (ganchos futuros)

- **Gastar em moeda estrangeira** (converter → debitar do caixa $Money): o
  ledger continua mono-moeda; um helper `ConvertAndWithdraw` é o gancho natural.
- **Taxas de câmbio flutuantes** (mercado de moedas como mini-jogo): a deriva
  hoje vem só da inflação; um `ExchangeRateNoise` opcional poderia compor por
  cima do `UnitsPerMoney`.
- **Eventos macroeconômicos** (crises, choques): viriam como chance cards
  globais alterando `GlobalAnnualPercent` temporariamente.

## 9. Testes

`tests/EconomySystem.Tests/`: `SimulationCalendarTests`, `InflationEngineTests`
(composição anual exata, ativação após 1 ano, independência dos índices),
`CurrencyMarketQuoteTests` (exemplo do carro, arredondamento),
`CurrencyRegistryTests`, `CurrencyMarketTests`, `MarketStateSerializerTests`
(round-trip preserva índices; continuar a simulação após o load é idêntico a
nunca ter salvo). Demo: seção "Mercado financeiro (v4)" em
`samples/EconomySystem.Demo`.
