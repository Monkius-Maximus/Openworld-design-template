# Integração dos sistemas: o mundo jogável

Camada que amarra **todos** os núcleos do repositório num mundo 3D jogável:
movimentação (Project Zomboid), relacionamentos (The Sims 2/4), economia +
mercado (v1–v4) e construção — mantendo o padrão da casa: **lógica pura e
testável em `src/WorldSimulation.Core`**, camada fina de engine em
`godot/src/game/`.

## Arquitetura

```
src/WorldSimulation.Core/            # integração pura (sem Godot)
├── WorldClock.cs                    # minutos de jogo → hora cheia, 3 normalizações/dia, meia-noite
├── GameWorld.cs                     # fachada: matriz+resolver+wants&fears, domicílios+ticks+mercado,
│                                    #   interações da UI (Outcome, não exceção), log de eventos
├── DemoWorldFactory.cs              # mundo de demonstração: Jogador, Alice, Bruno, carreira gig, wants
├── InteractionOutcome.cs            # resultado de interação para a UI
└── WorldThresholds.cs               # horas de normalização, hora inicial, fundos de demo
tests/WorldSimulation.Tests/         # xUnit (relógio, ticks, presente pago, amizade+want, demo)

godot/src/game/                      # camada de engine
├── GameManager.cs                   # dono do GameWorld; avança o relógio por delta; ÚNICO dono
│                                    #   do Player.ControlEnabled (menus × construção × jogador)
├── Npc3D.cs                         # corpo + Area3D de proximidade + CharacterId do registro
├── InteractionMenu.cs               # interações sociais + presente pago; UI montada em código
├── GameHud.cs                       # relógio/caixa/aspiração, prompt [F], log de eventos
└── BuildController3D.cs             # construção 3D reutilizando PlacementGrid/Footprint do núcleo
```

### Como os sistemas se conectam

| Ligação | Mecanismo |
|---|---|
| Tempo → tudo | `GameManager._Process` converte delta real em minutos de jogo; `WorldClock` dispara normalização (8h/14h/20h) e a virada do dia |
| Dia → economia | `EconomyTickSystem.DailyTick(households, market)` (v4): avança calendário+inflação, paga salários, entrega contas (ter/qui), debita assinaturas, roda folha |
| Dia → relacionamentos | `RelationshipDecaySystem.DailyTick` (decay −2/dia, envelhece modificadores/sentimentos) |
| NPC ↔ relacionamento | `Npc3D.CharacterId` é a chave do `CharacterRegistry`/`RelationshipMatrix`; o menu chama `GameWorld.Perform` |
| Relacionamento ↔ economia | Presente pago via `PaidInteractionResolver` (debita o caixa ANTES do social); saldo insuficiente vira `Outcome`, não evento |
| Interação → wants & fears | `WantsAndFearsSystem.AttachTo(resolver)` reavalia os dois envolvidos após cada interação; barra de aspiração no HUD |
| Mercado ↔ mundo | `MarketController` (tecla M) carrega/salva o `CurrencyMarket`; o `GameWorld` recebe ESSE mercado injetado — o calendário do save é o calendário do mundo |
| Construção ↔ movimento | `BuildController3D` reutiliza `PlacementGrid`/`Footprint` (células de 1 m no XZ) e o `AimCalculator` (raio×plano) da mira; objetos têm colisão real |
| UI ↔ input | `GameManager` é o único que escreve `Player.ControlEnabled`: menu de interação ou painel de mercado abertos pausam jogador e construção |

### Decisões

- **Uma fonte de verdade para o dia**: quem conta dias é o
  `SimulationCalendar` do mercado (persistido no save); o `WorldClock` só
  cuida do intradia. Sem dois contadores para divergir.
- **Indisponível ≠ exceção**: a UI é exploratória; `GameWorld.Perform`
  devolve `InteractionOutcome` quando a pré-condição do núcleo falha. As
  exceções do resolver continuam valendo para violação de contrato.
- **`GameManager` por último na cena**: os `_Ready` dos irmãos anteriores
  (MarketController carrega o save; NPCs entram no grupo) já rodaram.

## Controles (cena World3D)

| Tecla | Ação |
|---|---|
| WASD / Shift / Alt / C | Mover / correr / sprint / agachar |
| Botão direito | Mirar (personagem encara o cursor) |
| Scroll / Q / E | Zoom / girar câmera 45° |
| **F** | Interagir com o vizinho próximo (abre o menu) |
| **B** | Modo construção (paleta à direita; R gira, X demole, clique coloca) |
| **M** | Painel de moedas/mercado (pausa o jogador) |
| Esc | Fecha o menu de interação |

## Passo a passo de teste (tudo integrado)

1. Abra `godot/` no **Godot 4.6 .NET**, **Build**, abra
   `res://scenes/World3D.tscn` e rode com **F6**.
2. **HUD/tempo**: no topo, dia/hora/caixa/aspiração. O relógio corre
   (padrão: 2 min de jogo por segundo real → 1 dia ≈ 12 min). No log
   (canto inferior esquerdo), espere a virada do dia: salário do
   entregador entra no caixa e, ter/qui, chega conta (se houver bens).
3. **Interação social**: ande até a **Alice** (cápsula rosa) — aparece
   "[F] Interagir com Alice". Pressione **F**: o menu mostra as duas
   direções do relacionamento. `Conversar` algumas vezes → *Elogiar*
   destrava (exige daily ≥ 20) → *Flerte* (a atração inicial ajuda).
   Converse até o daily mútuo ≥ 50: o log anuncia a amizade **e** o
   desejo realizado (+25 de aspiração no HUD).
4. **Economia na interação**: `Dar Presente ($M75)` debita o caixa (veja
   o HUD) e aplica o modificador "Recebeu um presente". Esgote o caixa e
   o botão desabilita.
5. **Bruno** (cápsula verde, Nice 2): insulte-o para ver fúria,
   sentimento `Resentful` e — se insistir — a flag de inimigo realizar o
   **medo** do jogador (aspiração cai).
6. **Construção**: **B** abre a paleta; escolha "Mesa (2x1)", **R** gira,
   clique coloca (cursor verde = válido, vermelho = ocupado), **X**
   demole. Ande contra a mesa: o personagem **colide** com ela.
7. **Mercado**: **M** abre o painel de moedas — repare que o personagem
   **para de andar** enquanto ele está aberto. Crie uma moeda; o estado
   persiste em `user://market_state.json` e o **dia do calendário do
   save** é o mesmo do HUD.
8. Núcleo: `dotnet test` na raiz roda tudo, incluindo
   `WorldSimulation.Tests`.

## Ganchos para os assets finais

- NPCs: troque a cápsula/`Label3D` de cada `Npc3D` pelo modelo, mantendo
  `CollisionShape3D`, `Proximity` (Area3D) e `CharacterId`.
- Construção: o catálogo hardcoded do `BuildController3D` é o placeholder
  dos móveis reais — cada item vira um `PackedScene` com o mesmo
  footprint, como os `Placeable` do modo 2D.
- Novos personagens: registre no `DemoWorldFactory` (ou numa fábrica
  própria) e aponte o `CharacterId` do nó.
- O ritmo do tempo é o export `GameMinutesPerRealSecond` do GameManager.
