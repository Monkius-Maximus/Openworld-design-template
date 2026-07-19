# Auditoria de reúso para o jogo de futebol

> Auditoria somente-leitura (jul/2026). Verifica as premissas do plano de cópia
> (D27) e da hipótese de máquina de modos contra o código real. Nenhum arquivo
> foi modificado além deste relatório.

## V1 — Pureza dos núcleos: **CONFIRMADO**

Nenhum `src/*.Core` tem `using Godot` nem referência de pacote da engine. Os
cinco `.csproj` (`src/RelationshipSystem.Core`, `EconomySystem.Core`,
`MovementSystem.Core`, `BuildSystem.Core`, `WorldSimulation.Core`) só têm
`ProjectReference` entre núcleos: `src/EconomySystem.Core/EconomySystem.Core.csproj:14`
e `src/WorldSimulation.Core/WorldSimulation.Core.csproj:14-15`; nenhum
`PackageReference` de Godot. A única ocorrência da palavra "Godot" em `src/` é
um comentário (`src/BuildSystem.Core/GridCoord.cs:3`). Núcleos usam
`System.Numerics` (`src/MovementSystem.Core/MovementCalculator.cs:1`).

**Implicação:** a premissa central da cópia vale — os núcleos podem ser copiados
para o projeto de futebol sem arrastar a engine.

## V2 — Fronteira do motor de relacionamentos: **PARCIAL**

A fronteira funcional existe: `InteractionResolver` aceita registro nulo —
`src/RelationshipSystem.Core/InteractionResolver.cs:14` (`CharacterRegistry? _characters`)
e `:28` (`characters = null`); o único uso temático é `TopicBonus`
(`InteractionResolver.cs:94-98`), que retorna 0 sem registro.

**Motor genérico (porta por cópia):** `Relationship.cs`, `RelationshipMatrix.cs`,
`RelationshipValue.cs`, `RelationshipModifier.cs`, `RelationshipDecaySystem.cs`,
`Sentiment.cs`, `InteractionDefinition.cs`, `Interactions/InteractionLibrary.cs`,
`WantsAndFearsSystem.cs`, `AspirationMeter.cs`, `Desire.cs`,
`RelationshipDesires.cs` — nenhum referencia `CharacterTraits`, `Zodiac`,
turn-ons ou `AttractionCalculator` (grep sem ocorrências; as menções a
"Aspiration" em `WantsAndFearsSystem.cs:12-31` e `Desire.cs:47` são o
`AspirationMeter`/`AspirationValue` genéricos, não o enum temático).

**Camada temática Sims (não porta):** `CharacterTraits.cs` (enums `Zodiac` :3 e
`Aspiration` :9, `TurnOns` :70-78), `ZodiacCompatibilityTable.cs`,
`AttractionCalculator.cs`, `InterestCalculator.cs`, `CharacterRegistry.cs`.

**Divergências vs D27:** (1) `InteractionResolver.cs:98` chama
`InterestCalculator` diretamente — o motor listado como genérico no D27 tem uma
dependência de compilação da camada temática; a cópia exige levar
`InterestCalculator`/`CharacterRegistry`/`CharacterTraits` junto ou remover
`TopicBonus`. (2) `RelationshipThresholds.cs` mistura as camadas: os limiares
genéricos convivem com a classe temática `AttractionWeights`
(`RelationshipThresholds.cs:45-50` — `TurnOnWeight`, `SameAspirationBonus`,
`ZodiacCompatibilityMax`). D27 não menciona esse arquivo em nenhuma das listas.
A fronteira é limpa em *runtime*, mas não em *arquivos* — e a cópia é por arquivo.

## V3 — Inércia do eixo Y no movimento: **PARCIAL (adaptador basta, com 1 ressalva)**

- `MovementCalculator.DirectionFromInput` produz Y=0 (`MovementCalculator.cs:27-30`);
  `DirectionalSpeedMultiplier` achata em XZ (`:40,44`); `Accelerate` opera
  componente-a-componente (`:56-67`) — com Y=0 na entrada, Y=0 na saída.
- `StanceResolver` não usa vetores (`MovementStance.cs:19-35`), só bools e floats.
- `MovementThresholds` é só constantes (`MovementThresholds.cs`), nada de Y.
- `AimCalculator`: `YawFromDirection` usa só X/Z (`AimCalculator.cs:38-39`) e
  `TryYawTowards` achata (`:47`). **Exceção real:** `TryIntersectPlane`
  (`AimCalculator.cs:23-34`) usa Y de verdade — divide por `rayDirection.Y` —
  porque projeta o raio do mouse 3D num plano. Num jogo 2D essa função é
  simplesmente **não chamada** (o cursor já está no plano), não é forkada.

**Veredito:** um adaptador `Godot.Vector2(x,y) ↔ Vector3(x,0,z)` na camada de
integração 2D é suficiente; `TryIntersectPlane` fica sem uso (código morto
inofensivo na cópia), nenhum fork necessário.

## V4 — Câmera não porta: **CONFIRMADO (com um pedaço neutro)**

`CameraRigCalculator` é rig isométrico/ortográfico: `ApplyZoomSteps` opera sobre
`Camera.Size` ortográfico em passos multiplicativos
(`CameraRigCalculator.cs:38-44`, consumido em `godot/src/movement/IsoCameraRig.cs:73`);
`AimAnchor` é o look-ahead de mira no plano XZ (`:25-32`); as constantes são de
rig iso (`MovementThresholds.cs:35-54`). Fica fora da cópia.
**Pedaço neutro:** `SmoothFollow` (`CameraRigCalculator.cs:17-18`) é suavização
exponencial frame-rate-independent, componente-a-componente — matemática
dimensão-agnóstica reaproveitável isoladamente numa câmera 2D de futebol.

## V5 — Flags booleanas vs máquina de modos: **CONFIRMADO (a premissa da hipótese procede)**

Booleans de modo hoje: `Build.BuildModeActive` (`godot/src/game/BuildController3D.cs:24`),
`Build.InputEnabled` (`:27`), `_demolish` (`:44`), `Player.ControlEnabled` e o
derivado `uiOpen = Menu.Visible || MarketController.Panel.Visible`
(`godot/src/game/GameManager.cs:63-65`). A coordenação é manual por `if`
cruzado a cada frame (`GameManager.cs:63-69`) e repetida no input
(`GameManager.cs:81-85`: `!Menu.Visible && !MarketController.Panel.Visible &&
!Build.BuildModeActive`). Com 4 fontes independentes há 2⁴ combinações e só ~4
estados legítimos; nada impede `Menu.Visible && BuildModeActive` simultâneos —
hoje o GameManager mitiga zerando `InputEnabled`, mas `BuildModeActive`
permanece true com menu aberto (estado híbrido já existente). Adicionar "modo
partida" exigiria um quinto boolean consultado em todos esses pontos: partida
com `BuildModeActive == true` ou com o relógio do mundo avançando
(`GameManager.cs:57` roda incondicionalmente em `_Process`) seriam estados
impossíveis representáveis. A premissa do doc exploratório se confirma no
código — o que **não** valida o design proposto, que segue hipótese.

## V6 — Superfície de save: **inventário (sem decisão)**

**Persistido hoje** — só o mercado: `godot/src/MarketController.cs:28,56` grava
`user://market_state.json` via `MarketStateSerializer`
(`src/EconomySystem.Core/Market/MarketStateSerializer.cs:135-141`): dia do
calendário, inflação global e por produto, moedas, eventos econômicos. Único
`File.Write`/`user://` do repositório.

**Mutável e NÃO persistido** (tudo em `src/WorldSimulation.Core/GameWorld.cs`
salvo indicação): relacionamentos, modificadores e sentimentos
(`GameWorld.cs:25-26`); wants & fears + medidor de aspiração
(`GameWorld.cs:27`, `AspirationMeter.cs:12`); fichas de personagem
(`GameWorld.cs:24`); carreiras (`src/EconomySystem.Core/Household.cs:33`,
populadas em `DemoWorldFactory.cs:79-82`); fundos/households
(`GameWorld.cs:17-18`); relógio do mundo (`GameWorld.cs:32`, só o *dia* vai no
save do mercado, hora/minuto não); objetos colocados no mundo 3D
(`BuildController3D.cs:38-39` — `PlacementGrid` e `Dictionary<Guid, Node3D>`,
ambos só em memória).

**Implicação:** o futebol herdará núcleos majoritariamente sem serialização; a
futura D28 (e a decisão análoga do futebol, incluindo "salvar só no modo
mundo") parte de ~6 sistemas sem formato de save definido.

## V7 — Consistência do log: **PARCIAL (3 decisões pedem adendo, 1 violação direta)**

- **D08 (violada no código):** `godot/src/game/Npc3D.cs:33-34` conecta sinais
  Godot (`BodyEntered`/`BodyExited`) com `+=`, exatamente o que D08 proíbe.
  (`GameManager.cs:44` usa `+=` num evento C# puro — permitido.)
- **D10/D11 (pedem adendo, não contradição total):** a cena 2D canônica ainda
  existe e é a main scene (`godot/project.godot:9` → `Main.tscn`, que contém
  `TileMapLayer` e `CharacterBody2D` — `godot/scenes/Main.tscn:21,26`), mas o
  jogo "de verdade" vive na trilha 3D paralela (`godot/scenes/World3D.tscn`,
  `PlayerController3D : CharacterBody3D` em
  `godot/src/movement/PlayerController3D.cs:13`). O log não registra a decisão
  de ir a 3D nem o destino da trilha 2D.
- **D13 (escopo não implementado):** nenhuma ocorrência de `Hunger`/necessidades
  em `src/`, `godot/src` ou `tests/` (grep vazio). A decisão descreve um MVP de
  necessidades que não existe no código — adendo de status, não contradição.
- **D27 (parcial):** a partição declarada diverge do código em 2 pontos
  (ver V2: `InteractionResolver.cs:98` e `RelationshipThresholds.cs:45-50`).
- D09, D12, D15–D22 conferem com o código inspecionado; sem contradição achada.

## Top 5 riscos do plano de reúso (por impacto)

1. **A fronteira do D27 não é copy-paste-limpa** — `InteractionResolver.cs:98`
   depende de `InterestCalculator`, e `RelationshipThresholds.cs:45-50` embute
   pesos temáticos no arquivo de limiares genéricos. A "cópia do motor" exige
   uma micro-cirurgia não documentada; risco de levar a camada Sims junto por
   acidente ou de quebrar compilação ao excluí-la.
2. **Save quase inexistente** (V6): só o mercado persiste; relacionamentos,
   aspiração, carreiras, objetos e relógio são voláteis. O futebol precisa de
   carreira persistente por definição — a D28/decisão-irmã é bloqueante e o
   padrão `MarketStateSerializer` cobre ~15% da superfície.
3. **O contraexemplo de flags já degrada** (V5): o padrão atual admite estados
   híbridos hoje (`BuildModeActive` true com menu aberto) e o relógio avança em
   qualquer "modo". Copiar `GameManager.cs` como base do futebol importa o
   problema; a máquina de modos segue hipótese não decidida — vácuo de decisão.
4. **Disciplina do log erodindo** (V7): D08 já violada em `Npc3D.cs:33-34`,
   D10/D11 defasadas, D01–D07/D23–D26 em lacuna. Como o D14 faz do log o
   contrato, copiar núcleos guiando-se pelo log (e não pelo código) pode
   reproduzir premissas falsas no projeto novo.
5. **Y de verdade num único ponto** (V3): risco baixo — `TryIntersectPlane`
   (`AimCalculator.cs:23-34`) é 3D-real, mas basta não chamá-la no 2D. O risco
   é alguém "limpar" a cópia forkando o núcleo em vez de deixar a função ociosa,
   quebrando a paridade que torna a cópia barata.
