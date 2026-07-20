# Adendo — verificação cruzada com o repositório real do futebol (SoccerDreamGame)

> Complemento da `auditoria-reuso-futebol.md` (jul/2026), após inspeção de
> `Monkius-Maximus/SoccerDreamGame` @ `b6947f3`. Somente leitura; caminhos
> abaixo são relativos à raiz daquele repositório.

## A1 — A cópia D27 ainda não aconteceu (e o jogo seguiu outro caminho)

Nenhuma ocorrência de `InteractionResolver`, `RelationshipMatrix`,
`WantsAndFears`, `AspirationMeter`, `Sentiment` ou `RelationshipModifier` em
todo o repositório (grep vazio em `src/`, `game/`, `tests/`). O jogo tem
domínio próprio (`src/SoccerSim.Core/Domain/` — `Career.cs`, `Player.cs`,
`Team.cs`), economia própria (`src/SoccerSim.Core/Economy/EconomyTypes.cs`,
`sql/0004_economy.sql`) e um sistema de humor próprio e muito mais raso que o
motor do Openworld: `FormMood` com inteiros em [-5, +5] por temporada
(`sql/0003_form_mood.sql:8-18`).

**Implicação:** a premissa da auditoria original — "o futebol reutilizará
núcleos por cópia" — não foi exercida nem violada: está pendente. O ponto
natural de entrada da cópia é o life-sim, que hoje é um stub de 13 linhas
(`game/scenes/lifesim/LifeSimScene.cs:9-13`). Mas o projeto já cria primitivas
paralelas (`FormMood` vs `Sentiment`/`RelationshipMatrix`; economia própria vs
`EconomySystem.Core`) — cada dia de evolução delas encarece ou inviabiliza a
cópia D27.

## A2 — A máquina de modos existe, confirma o V5 e diverge da hipótese

O padrão de flags booleanas foi de fato substituído: grep por
`bool.*Active|Enabled|Open` em `game/` retorna vazio. A máquina é núcleo puro
com tabela de transições e fail-fast — `src/SoccerSim.Core/Modes/ModeStateMachine.cs:21-28`
(adjacência `Allowed`) e `:44-56` (`TransitionTo` lança em transição ilegal) —
embrulhada por um autoload fino que troca cena por modo
(`game/autoload/GameModeManager.cs:23-29`). Isso valida a premissa do V5 no
projeto onde ela importava.

Divergências relevantes vs `exploracao-maquina-de-modos.md`:

1. **4 modos, não 2**: `Loading` (hub), `Calendar`, `LifeSim`, `Match`
   (`src/SoccerSim.Core/Modes/GameMode.cs`), sem transição direta
   `Match ↔ LifeSim` (`ModeStateMachine.cs:24-27`).
2. **Sem `IGameMode`/`HandoffPayload`**: a máquina é sobre um enum; o "payload"
   é a propriedade `ActiveFixture` no autoload (`GameModeManager.cs:36`) na
   ida, e o banco SQLite na volta — não o handoff tipado do doc.
3. **A regra "a partida não escreve nos núcleos" NÃO vale**: a partida
   renderizada persiste o resultado diretamente —
   `src/SoccerSim.Core/Simulation/MatchPresentation.cs:61` chama
   `_gateway.SaveResult(...)`, que insere gols e incrementa standings
   (`src/SoccerSim.Infrastructure.Sqlite/SqliteFixtureGateway.cs:112`). O
   "tradutor MatchResult → mundo" do doc não existe (o mundo/life-sim é stub).

O `MatchResult` do doc existe com outro shape:
`src/SoccerSim.Core/Simulation/SimulationTypes.cs:25` (record) e é o mesmo
objeto em todos os tiers de simulação (`MatchEngine.cs:76,135`).

## A3 — Pureza do núcleo: confirmada, mas por reimplementação, não por cópia

`SoccerSim.Core.csproj` é `Microsoft.NET.Sdk` puro, zero dependências
(`src/SoccerSim.Core/SoccerSim.Core.csproj`), e `grep "using Godot"` em `src/`
é vazio. O mesmo molde nucleus + integração do Openworld foi reproduzido do
zero — inclusive rejeitando `System.Numerics`: o núcleo usa um `Vec2` próprio
em double (`src/SoccerSim.Core/Pitch/Vec2.cs:4-11`), com conversão para
`Godot.Vector2` só na fronteira de render.

**Implicação para o V3 da auditoria:** o veredito "adaptador
`Vector2 ↔ Vector3(x,0,z)` basta" ficou sem cliente — o futebol não copiou
`MovementSystem.Core` e resolveu vetores por conta própria. Se o life-sim
isométrico (prometido em `LifeSimScene.cs:6-7`) vier a copiar o movimento do
Openworld, haverá **duas** convenções de vetor no mesmo projeto (`Vec2` double
do pitch vs `System.Numerics.Vector3` float do núcleo copiado).

## A4 — Save: o futebol resolveu (diferente) o que o Openworld deixou em aberto

Enquanto o Openworld persiste só o mercado em JSON (V6), o futebol nasceu com
SQLite: `user://save.db` (`game/autoload/SaveServiceNode.cs:11`), migrações
numeradas embutidas (`sql/0001`–`0005`), carreira persistente singleton
(`sql/0005_career.sql:8-12`), form/humor por temporada (`sql/0003`), economia
(`sql/0004`). Há até proteção de idempotência no save de resultado
(`MatchPresentation.cs:55`).

**Implicação:** o insumo da futura D28 muda de natureza. Se os núcleos do
Openworld forem copiados para o futebol, eles chegam **sem serialização**
(V6) num projeto cujo padrão é repositório SQLite — a cópia exigirá escrever
repositórios/DTOs SQLite para relacionamentos, sentimentos e aspiração, custo
que o D27 não menciona.

## Vereditos cruzados (resumo)

| Premissa da auditoria | Situação no SoccerDreamGame |
|---|---|
| Núcleos serão copiados (D27) | **Pendente** — nada copiado; primitivas paralelas crescendo (A1) |
| Flags não escalam → máquina de modos (V5) | **Confirmada na prática** — máquina pura + fail-fast, zero flags (A2) |
| Design do doc exploratório | **Parcial** — máquina existe, mas sem `IGameMode`/handoff, e a partida escreve no banco (A2) |
| Adaptador 2D basta (V3) | **Sem cliente** — movimento não foi copiado; `Vec2` próprio (A3) |
| Save é a lacuna crítica (V6/risco 2) | **Confirmada às avessas** — futebol já tem SQLite; a lacuna vira "núcleos copiados sem camada SQLite" (A4) |

**Risco novo nº 1 (atualiza o Top 5):** deriva de primitivas — quanto mais o
life-sim evoluir sobre `FormMood`/economia própria antes da cópia D27, maior a
chance de a cópia nunca acontecer ou de coexistirem dois sistemas de
humor/economia incompatíveis no mesmo jogo.
