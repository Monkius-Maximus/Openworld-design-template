# Etapa de Batalhas — design, alinhamento e migração Unreal → Godot

Este documento reorganiza a etapa de batalhas do projeto: consolida **o que
foi planejado × o que foi entregue**, formaliza a spec do combate (que até
aqui não existia por escrito), registra a decisão de **migrar da Unreal para
a Godot Engine** e define o roadmap v1–v3. A v1 está implementada em
`src/BattleSystem.Core` (testes em `tests/BattleSystem.Tests`, demo em
`samples/BattleSystem.Demo`, adaptador Godot em `godot/`).

---

## 1. Alinhamento: planejado × entregue

| Etapa | Planejado | Situação antes | Situação agora |
|---|---|---|---|
| Relacionamentos | Spec Partes 1–7 (TS2/TS4/CK3) | ✅ entregue (PR #1 + melhorias) | inalterado |
| Economia | Pesquisa TS2 + roadmap v1–v3 | ✅ entregue (PR #4) | inalterado |
| Integração com engine | "Parte 8 da spec" (era Godot, depois pivotou-se a Unreal) | ❌ **nunca entregue** — omitida do build | ✅ entregue em `godot/` (adaptador fino, fora do CI) |
| **Etapa de batalhas** | Citada no plano geral, **sem spec escrita** | ❌ nada no repositório | ✅ esta spec + **v1 funcional sem assets** |

O débito real da etapa de batalhas era duplo: (a) não havia documento de
design — só intenção; (b) a parte de engine nunca saiu do papel. Os dois
são resolvidos aqui, já na engine definitiva.

## 2. Decisão de engine: Unreal → Godot

Motivação: alinhamento com a equipe parceira. Custo de migração: **baixo**,
porque o repositório sempre manteve a regra de jogo em C#/.NET 8 puro — e a
Godot 4 (.NET) roda exatamente esse runtime. Nenhum módulo (`Relationship`,
`Economy`, `Battle`) precisa de porte; o que muda é só a casca.

### Padrões arquiteturais que mudam

| Unreal (como seria) | Godot (como fica) | Efeito no código |
|---|---|---|
| Actor/Pawn + ActorComponent | Node + composição por cenas | nenhum: o núcleo não tem atores |
| GameMode / GameState | autoload (singleton) ou nó controlador | adaptador (`BattleController`) |
| Delegates / Event dispatchers | eventos C# → `[Signal]` | já era evento C#; o adaptador re-emite |
| GameplayAbility System (GAS) | **não portar o GAS**: `BattleActionDefinition` declarativa | regra fica testável fora da engine |
| DataAsset / DataTable | catálogos estáticos; promovíveis a `Resource` | `BattleActionLibrary` |
| UPROPERTY/reflection p/ tuning | `Resource` exportado lendo `BattleThresholds` | futuro (v2) |
| Build C++/Blueprint híbrido | C# único, mesmo SDK do CI | um toolchain só |

Princípio (igual ao da economia): **núcleo agnóstico + ponte fina**. A engine
nunca é dependência do domínio; a Godot consome o domínio.

## 3. Arquitetura da v1 — espelhando os padrões existentes

Cada peça da batalha reusa um padrão já provado no repositório:

| Batalha | Análogo no núcleo existente |
|---|---|
| `CombatantStats` (validação fail-fast nos `init`) | `CharacterTraits` |
| `Combatant` (vida/stamina com setter privado + clamp) | `RelationshipValue` |
| `StatusEffect` (nomeado, temporário, em **turnos**) | `RelationshipModifier` (CK3, em horas) |
| atributos efetivos ignoram status expirados | `EffectiveDaily` ignora modificadores expirados |
| templates de status **clonados ao aplicar** | clonagem de modificadores no `InteractionResolver` |
| `BattleActionDefinition` (Available/HitChance/OnHit/OnMiss) | `InteractionDefinition` (Available/Accepted/OnAccept/OnReject) |
| `BattleResolver` (executa, falha rápido, emite eventos) | `InteractionResolver` |
| `TurnSystem` (veneno, envelhecimento, regen, iniciativa) | `RelationshipDecaySystem` / `EconomyTickSystem` |
| `BattleThresholds` / `CombatPhysics` / `MoraleRules` | `RelationshipThresholds` / `RelationshipPhysics` |
| `Actions/BattleActionLibrary` | `Interactions/InteractionLibrary` |
| `Integration/RelationshipBattleBridge` (só lê a matriz, escreve por API pública) | `Integration/RelationshipEconomyBridge` |

### Modelo de combate (v1)

- **Encontro por turnos** entre dois times (`Battle`): iniciativa por
  velocidade decrescente, derrotados são pulados, rodada avança quando a
  iniciativa dá a volta.
- **Ação declarativa**: custo de stamina (falha rápido se não paga), tipo de
  alvo (inimigo/aliado/si), pré-condição `Available`, chance `HitChance`
  [0..1] e efeitos separados de acerto/erro (errar um golpe pesado deixa
  "Desequilibrado").
- **Dano** = base da ação + ataque efetivo − defesa efetiva, nunca abaixo de
  `CombatPhysics.MinDamage`. Cura não ressuscita.
- **Status por turno**: bônus de ataque/defesa e dano por turno (veneno),
  aplicados no fim do turno do portador pelo `TurnSystem`, que também regenera
  stamina e avança a iniciativa.
- **IA heurística** (`SimpleBattleAI`): cura-se em perigo (≤ 25% de vida) →
  maior dano pagável no inimigo mais ferido → repõe stamina → defende. É o que
  torna o sistema **funcional hoje, sem assets nem input**: console e Godot
  rodam batalhas completas sozinhos.
- **Aleatoriedade injetável**: `BattleResolver(battle, new Random(seed))` para
  testes e replays determinísticos.

### Integração com relacionamentos (sem editar o núcleo)

`RelationshipBattleBridge`, como na economia, é a costura única:

- **Antes da batalha** a matriz vira moral: lutar ao lado de um amigo
  (`AreFriends`) dá `+ataque`; enfrentar um rival (daily ≤ `Enemy`) dá fúria
  (`+ataque`, `−defesa`) — ambos como `StatusEffect` que dura a batalha.
- **Depois da batalha** o desfecho vira memória: perdedor → vencedor recebe
  "Fui derrotado em combate" (−15, 48h) e vencedores aliados trocam "Lutamos
  lado a lado" (+10, 72h) — reusando o mecanismo de modificador do CK3. Uma
  batalha pode, portanto, criar inimizades reais (o modificador derruba o
  `EffectiveDaily`) e cimentar amizades.

## 4. Funcional sem assets (estado atual)

- `dotnet run --project samples/BattleSystem.Demo` — batalha 2×2 completa no
  console: moral pré-batalha, IA nos dois lados, log de eventos e memórias
  escritas de volta na matriz.
- `godot/` — mesmo cenário na engine: barras de vida, log e botão "Próximo
  turno", com **UI 100% construída em código** (nós nativos). Nenhuma textura,
  modelo ou som é necessário.
- `dotnet test` — 29 testes cobrem stats, dano/cura/stamina, status,
  iniciativa, resolver (fórmula, erro, clonagem, eventos, fail-fast), turno
  (veneno encerra batalha), ponte e IA.

### Assets open source para quando a arte entrar

| Fonte | Conteúdo | Licença |
|---|---|---|
| [Kenney](https://kenney.nl) | sprites 2D, UI packs, SFX, protótipos 3D | CC0 |
| [KayKit — Kay Lousberg](https://kaylousberg.itch.io) | personagens/cenários 3D low-poly com animações (esqueleto compatível entre packs) | CC0 |
| [Quaternius](https://quaternius.com) | modelos 3D animados (RPG, monstros) | CC0 |
| [game-icons.net](https://game-icons.net) | milhares de ícones de habilidade/status | CC BY 3.0 (creditar) |
| [OpenGameArt](https://opengameart.org) | 2D/3D/áudio (filtrar por licença) | CC0/CC-BY variadas |
| [Freesound](https://freesound.org) | SFX (filtrar CC0) | CC0/CC-BY |
| [Maaack's Game Template (Godot)](https://github.com/Maaack/Godot-Game-Template) | menus, settings, cenas-base | MIT |

Regra prática: preferir CC0; com CC-BY, manter um `CREDITS.md` no projeto Godot.

## 5. Roadmap

**v1 — fundação (✅ entregue, este PR):** núcleo por turnos completo +
ponte de relacionamentos + IA + demo console + adaptador Godot sem assets.

**v2 — economia e profundidade tática (planejado):**
- Equipamento comprado com § (`OwnedObject` já deprecia — armas se desgastam
  pelo mesmo mecanismo); loot e recompensas creditados via `HouseholdFunds`.
- Ponte `EconomyBattleBridge` (dependência unidirecional
  `BattleSystem → EconomySystem`, espelhando a regra existente).
- Cooldowns por ação e barra ATB opcional (tempo semi-real) — o núcleo já
  separa resolver/turno, então é trocar o `TurnSystem`.
- `Resource` Godot exportando `BattleThresholds` para tuning no editor.

**v3 — narrativa e mundo aberto (planejado):**
- Moral dinâmica por **sentimentos** (TS4): `Hurt`/`Bitter` alimentam fúria;
  vitórias geram `Motivated` via API pública.
- Wants & Fears de combate ("derrotar meu rival", "medo de perder para X")
  plugando no `WantsAndFearsSystem` existente.
- Recrutamento pós-batalha gated por relacionamento (como promoções da
  economia usam `CountFriends`).
- Encontros no mundo aberto: spawn por região/horário no lado Godot, regra no
  núcleo.

## 6. Como rodar

```bash
dotnet test                                        # inclui BattleSystem.Tests
dotnet run --project samples/BattleSystem.Demo     # batalha completa no console
# Godot: abrir godot/project.godot na Godot 4.3+ (.NET) e rodar a cena principal
```
