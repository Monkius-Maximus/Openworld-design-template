# Sistema de Economia — design ("simplicidade amplificada" à la The Sims 2)

Este documento especifica a camada **econômica/financeira** do projeto. Ela
**reaproveita** os padrões do núcleo de relacionamentos (`RelationshipSystem.Core`)
e se integra a ele, num projeto irmão `EconomySystem.Core`.

> Filosofia: replicar a **simplicidade amplificada** de The Sims 2 — uma única
> moeda, sem inflação nem câmbio, com um loop econômico fechado e legível
> (**ganhar → guardar → gastar → o tempo passa**) — em vez de simular
> macroeconomia. A modernização (gig economy, assinaturas, marketplace) é
> deixada para fases posteriores, mas o esquema já abre espaço para ela.
> A v4 adiciona uma camada **opt-in** de mercado financeiro (moedas, câmbio e
> inflação) por cima deste núcleo, sem alterá-lo — ver
> [mercado-financeiro-design.md](mercado-financeiro-design.md).

## 1. Pesquisa de referência

### Como The Sims 2 trabalha a economia
- **Moeda única**: o Simoleon (§), inteiro. **Caixa compartilhado por domicílio**
  (um único bolso para a família). Sem inflação, sem múltiplas moedas — os preços
  são deliberadamente "errados" (uma SUV §4.000, uma pizza §40): a simplicidade é
  uma escolha de design.
- **Receita**: carreiras (salário diário por nível; promoção exige habilidades +
  número de **amigos do domicílio** + humor); **freelance por habilidade**
  (pinturas, jardinagem/colheita, artesanato); **Open for Business** (comprar
  estoque → vender com markup; fidelidade de clientes em 5 estrelas; funcionários
  com papéis Restocker/Caixa/Vendas; ranking + perks); *chance cards* (eventos
  aleatórios no trabalho); **pontos de aspiração** como moeda paralela "soft".
- **Despesas**: **contas** calculadas a partir do valor total dos objetos do lote
  (com depreciação), **−10% por filho**, entregues terça/quinta; o **repo-man**
  recolhe bens e gera memória ruim se não pagas; compra de objetos (que
  depreciam); salário de funcionários; reposição de estoque.
- **Patrimônio líquido** = caixa + valor dos bens.

Fontes: [Simoleon](https://sims.fandom.com/wiki/Simoleon),
[Bill](https://sims.fandom.com/wiki/Bill),
[Open for Business](https://sims.fandom.com/wiki/The_Sims_2:_Open_for_Business),
[Careers (StrategyWiki)](https://strategywiki.org/wiki/The_Sims_2/Careers),
[Aspiration reward](https://sims.fandom.com/wiki/Aspiration_reward_(The_Sims_2)).

### Como jogos similares resolveram a área
- **Stardew Valley**: cadeia produtiva (matéria-prima → bens artesanais de maior
  valor: vinho, queijo) + sazonalidade. Lição: dar **profundidade por
  transformação de valor**, não por simulação de mercado.
- **Animal Crossing**: loop "vender → gastar em upgrades", e a *stalk market* como
  mini-jogo opcional de risco. Lição: o **gasto** precisa ser tão satisfatório
  quanto o ganho.

Aplicação aqui: manter o loop fechado e legível; profundidade vem do **negócio
próprio** (transformar custo de estoque em lucro) e da **integração social**, não
de modelar inflação.

## 2. Escopo por fase

| Fase | Conteúdo | Estado |
|------|----------|--------|
| **v1** | Caixa + contas + bens com depreciação; renda freelance; negócio próprio (OFB); tick diário; integração cliente↔relacionamento | Implementada |
| **v2** | Carreiras + promoções (habilidades + **amigos**, via `AreFriends`) + humor; camada digital (assinaturas, gig, marketplace); presentes pagos | Implementada |
| **v3** | Perks de ranking do negócio; folha de pagamento dos funcionários no tick; chance cards; moeda "soft" (pontos de aspiração + objetos de recompensa) | Implementada |
| **v4** | Mercado financeiro opt-in: moeda base $Money nomeada; inflação global e local (por produto/por moeda); registro de moedas com câmbio; persistência JSON; UI Godot — ver [mercado-financeiro-design.md](mercado-financeiro-design.md) | Implementada |

### Mapa da v3 (aprofundamento + moeda soft)

- **Perks de ranking** (`Businesses/BusinessPerk.cs`): conforme as estrelas de
  fidelidade sobem, o negócio desbloqueia perks (`BusinessPerks.ForRank`). O perk
  `WholesaleDiscount` (rank ≥ 2) dá desconto real na reposição, aplicado em
  `BusinessResolver.Restock`.
- **Folha de pagamento**: `BusinessResolver.PayEmployees` debita a soma dos
  salários; o `EconomyTickSystem` a paga a cada dia para os negócios do domicílio.
- **Chance cards** (`ChanceCard` + `Careers/ChanceCardLibrary.cs`): evento de
  trabalho com DUAS opções (visão moderna sobre o TS2, que tinha desfecho fixo);
  `CareerResolver.ResolveChanceCard` aplica caixa + efeito de carreira
  (promover/rebaixar) e emite evento.
- **Moeda "soft"** (`AspirationWallet` + `AspirationReward` +
  `Aspiration/AspirationRewardCatalog.cs`): pontos de aspiração, totalmente
  separados dos Simoleons (como a atração é um eixo à parte). `AspirationResolver`
  resgata objetos de recompensa — incluindo a "Árvore do Dinheiro", que liga a
  moeda soft de volta ao caixa.

### Mapa da v2 (carreiras + modernização)

- **Carreiras**: `CareerDefinition`/`CareerLevel`/`CareerState`/`CareerResolver`
  (raiz) + catálogo `Careers/CareerLibrary.cs`. Promoção avalia os requisitos do
  **próximo** nível (habilidades + amigos + humor — gates do TS2). O número de
  amigos vem do `RelationshipMatrix` via
  `RelationshipEconomyBridge.EvaluatePromotion`/`CountFriends`.
- **Modernização** (além do Ocidente dos anos 2000): pistas `Remote` e `Gig` ao
  lado da `Traditional` (ex.: `RemoteSoftware`, `GigCourier`); `Subscription`
  (micro-conta recorrente: celular/streaming) debitada pelo tick;
  `Digital/DigitalIncomeLibrary.cs` (bico por app, marketplace) — apenas novas
  instâncias de `IncomeActivityDefinition`, provando que o esquema generaliza.
- **Presentes pagos**: `Integration/PaidInteractionResolver.cs` envolve o
  `InteractionResolver` do núcleo — debita o custo e só então aplica o efeito
  social; sem fundos, a interação é cancelada. Sem editar o núcleo.
- **Tick estendido**: paga salários dos moradores empregados e debita assinaturas
  vencidas, mantendo o loop fechado.

## 3. Modelo de dados e fórmulas (v1)

Moeda = `int` Simoleons (§). Tudo inteiro para evitar drift de ponto flutuante.

- **Caixa** (`HouseholdFunds`): saldo nunca negativo. `TryWithdraw` falha se
  faltar saldo — é o gatilho do repo-man. (Crédito/dívida fica para v2.)
- **Bem** (`OwnedObject`): `CurrentValue` parte de `PurchasePrice` e deprecia rumo
  a um **piso de revenda** = `SalvageFloorPercent`% (default 40%).
  `passo_diário = PurchasePrice × DepreciationPercentPerDay% (default 2%)`.
  Mecânica análoga a `RelationshipValue.DecayDailyTowardZero`, mas o alvo é o piso.
- **Conta**:
  `conta = valor_faturável_dos_bens × BillRatePercent% (default 3%)`,
  depois `× (100 − min(90, filhos × 10))%`. Entregue em terça/quinta; se não paga,
  inicia tolerância de `RepoManGraceDays` (default 2); ao zerar, repo-man.
- **Renda freelance** (`IncomeActivityDefinition`): schema declarativo
  `Available` (habilidade mínima) + `Succeeds` (chance por habilidade) +
  `OnSuccess/OnFailure` (template de transação clonado ao aplicar).
- **Negócio** (`Business`): `preço = custo × (1 + markup%)`,
  `lucro = preço − custo`. `markup` limitado por `MaxMarkupPercent`. Fidelidade em
  estrelas a partir do gasto acumulado (`LoyaltyStarThresholds`).
- **Patrimônio líquido** = `Funds.Balance + Inventory.TotalObjectValue`.

## 4. Arquitetura — espelhando o núcleo de relacionamentos

| Peça (economia) | Análogo (relacionamento) | Padrão reusado |
|---|---|---|
| `MoneyTransaction` | `RelationshipModifier` | registro nomeado/justificado, validado no `init`, clonado ao aplicar |
| `HouseholdFunds` | `RelationshipValue` | setter privado + métodos delta + clamp (em 0) |
| `OwnedObject.DepreciateOneDay` | `RelationshipValue.DecayDailyTowardZero` | passo rumo a um piso |
| `HouseholdInventory` | `RelationshipMatrix` | dicionário + Get/TryGet/All/Count |
| `IncomeActivityDefinition` | `InteractionDefinition` | schema declarativo Available/Succeeds/Outcomes |
| `IncomeResolver` / `BillsSystem` | `InteractionResolver` | fail-fast + clona template + emite eventos |
| `EconomyTickSystem` | `RelationshipDecaySystem` | DailyTick sobre toda a store |
| `EconomyThresholds`/`Physics`/`BusinessRules` | `RelationshipThresholds`/`Physics`/`AttractionWeights` | um arquivo, classes `static` por preocupação |
| `IncomeActivityLibrary` | `Interactions/InteractionLibrary` | catálogo estático por Id, `Random.Shared` |
| `MoneyEventHandler` etc. | delegates em `InteractionResolver` | eventos por delegate |

Dependência **unidirecional**: `EconomySystem.Core` → `RelationshipSystem.Core`
(nunca o contrário). O núcleo de relacionamentos permanece livre de dependências.

## 5. Integração com relacionamentos

Toda a costura vive em `Integration/RelationshipEconomyBridge.cs`, que só **lê** o
`RelationshipMatrix` e **escreve de volta** por APIs públicas (`AddModifier`,
`Value`) — sem editar o núcleo:

1. **Cliente É relacionamento** (integração principal da v1): a disposição de
   compra deriva de `Get(cliente, dono).EffectiveDaily`; uma venda satisfatória
   devolve um modificador "Bom atendimento" ao relacionamento (mecanismo CK3
   reusado). Fidelidade (estrelas) e amizade co-evoluem.
2. **Repo-man → memória ruim**: ao acionar o repo-man, anexa um modificador
   negativo temporário entre os moradores.
3. **(v2) Promoções gated por amigos**: `RelationshipEconomyBridge.CountFriends`
   já reusa `RelationshipMatrix.AreFriends` — pronto para quando carreiras entrarem.

## 6. Modernização (v2+) — além do Ocidente dos anos 2000

The Sims 2 retrata a sociedade ocidental do início dos anos 2000 (sem smartphones).
A camada moderna, opcional e enxuta, **reusa o mesmo schema** (sem máquinas novas):
- **Assinaturas digitais** (`Digital/Subscription.cs`): micro-contas recorrentes
  (plano de celular, streaming), debitadas pelo tick.
- **Gig economy / marketplace** (`Digital/DigitalIncomeLibrary.cs`): apenas novas
  instâncias de `IncomeActivityDefinition` (entrega por app, venda online),
  provando que o esquema generaliza.
- **Carreiras remotas/gig** como uma `CareerTrack` ao lado das tradicionais.
- **Pagamento digital/crédito**: apenas sabor (`PaymentMethod`) na v2; sem modelar
  juros — coerente com a "simplicidade amplificada".

## 7. Verificação

- `dotnet build` da solução (inclui `EconomySystem.Core` + `EconomySystem.Tests`).
- `dotnet test` — a suíte de economia cobre caixa, depreciação, contas/repo-man,
  resolver de renda, negócio (restock/venda/fidelidade) e o bridge
  (CountFriends, disposição de compra, modificador de bom atendimento).
- O núcleo de relacionamentos continua intacto (sem regressão).
