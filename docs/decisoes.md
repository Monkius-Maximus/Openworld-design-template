# Registro de decisões — Openworld (D01–D34, reconstruído)

> **Reconstruído em 18/out/2026.** O log original das decisões D01–D34 foi escrito em conversas
> e nunca foi commitado; o texto se perdeu. Este arquivo junta o que dá para provar, e só isso.
> Ele substitui o log perdido como referência, mas **não** finge ser o texto original.

## Como ler

Cada entrada tem um estado:

- **Reconstruída** — o conteúdo vem de evidência escrita (código, commit, documento do repositório
  ou registro do autor) e o número vem de uma fonte que o cita.
- **Bloco reconstruído** — o conteúdo do bloco é conhecido, mas não dá para dizer qual decisão
  ocupava qual número dentro dele. Nenhum número individual foi inventado.
- **Lacuna** — o número existiu, mas o conteúdo não foi recuperado.

Fontes, citadas em cada entrada:

| Sigla | Fonte |
|---|---|
| `AUD` | `auditoria-reuso-futebol.md` (jul/2026, branch `claude/auditoria-reuso-futebol-624n5m`), que conferiu o log contra o código. |
| `ADD` | O adendo da mesma auditoria contra o SoccerDreamGame. |
| `REG` | Registro de trabalho do autor nas sessões de design (fora do repositório). |
| Código | Caminho e commit citados na entrada. |

Regras daqui em diante:

1. **Nenhuma entrada é renumerada.** Se uma lacuna for recuperada, ela entra no seu número.
2. Uma decisão mudada ganha número novo, que cita a antiga. O texto antigo não é editado.
3. **As próximas decisões começam em D40.** D35–D39 foram rascunhos da ponte com o
   SoccerDreamGame; viraram o ADR-0020 daquele repositório e não são reutilizados aqui.
4. No SoccerDreamGame, estas decisões são citadas com o prefixo `OW-` (`OW-D27`), segundo o
   ADR-0016 de lá.

---

## D01–D07 — Lacuna

Já faltavam no log em julho (`AUD` V7, risco 4).

## D08 — Sinais Godot só via `Connect` · Reconstruída

Sinais Godot são conectados com `Connect(SignalName.X, Callable.From<T>(Metodo))`, nunca com `+=`.
Eventos C# puros (por exemplo, `GameWorld.EventLogged`, um `event Action<string>`) continuam
usando `+=`: não são sinais.

- Fontes: `AUD` V7; `REG`.
- Estado: estava violada em 6 pontos de `godot/src/game/` (`Npc3D.cs:33-34`,
  `InteractionMenu.cs:56,65,76`, `BuildController3D.cs:68`). Corrigida no commit
  "Sinais Godot via Connect".

## D09 — Lacuna

Existia e conferia com o código em julho (`AUD` V7); o conteúdo não foi recuperado.

## D10–D11 — Cena 2D canônica · Bloco reconstruído

A cena canônica é 2D: `TileMapLayer` (nunca o `TileMap` obsoleto) para o piso e
`CharacterBody2D` para o jogador, em `godot/scenes/Main.tscn` (main scene em
`godot/project.godot:9`).

- Fontes: `AUD` V7; `REG` (regra `TileMapLayer` exclusivamente).
- **Adendo pendente** (`AUD` V7): o jogo "de verdade" passou a viver numa trilha 3D paralela
  (`godot/scenes/World3D.tscn`, `PlayerController3D : CharacterBody3D`), e o log não registra
  essa ida a 3D nem o destino da trilha 2D. O branch não mergeado
  `claude/cell-construction-mode-cjmskr` voltou a trabalhar em 2D isométrico em julho (ver
  D23–D26). **Falta decidir qual trilha é a canônica.** Essa decisão deve entrar como D40+.

## D12 — Lacuna

Existia e conferia com o código em julho (`AUD` V7); o conteúdo não foi recuperado.

## D13 — MVP de necessidades · Reconstruída (escopo)

O MVP inclui um sistema de necessidades (`Hunger` e afins).

- Fontes: `AUD` V7.
- Estado: **não implementado** — nenhuma ocorrência em `src/`, `godot/src` ou `tests/`.
- Ponto de integração conhecido: `WorldClock` (`src/WorldSimulation.Core/WorldClock.cs`) já
  expõe `HourElapsed`, `NormalizationDue` e `DayElapsed`, usados pelo decaimento de
  relacionamentos e pelo tick de economia (`REG`).

## D14 — O log é o contrato · Reconstruída

As decisões registradas são vinculantes: o código segue o log, e uma divergência vira entrada
nova, não um ajuste silencioso.

- Fontes: `AUD` risco 4 ("como o D14 faz do log o contrato"); `REG` (decisões confirmadas são
  tratadas como contrato nos artefatos seguintes).
- Consequência: este arquivo existe porque o contrato estava fora do repositório.

## D15–D16 — Lacuna

Existiam e conferiam com o código em julho (`AUD` V7: "D09, D12, D15–D22 conferem").

## D17–D22 — Criador de personagem (CAS) · Bloco reconstruído

Conteúdo do bloco (fonte: `REG`). Não há código de CAS no repositório.

- **Base mesh**: Quaternius Universal Base Characters (CC0) é a recomendação principal;
  MPFB2/MakeHuman (saída CC0, morfologias como shape keys) é candidato a fluxo híbrido.
- **Nomes dos blend shapes**: só sobrevivem a Blender → glTF → Godot via
  `mesh.extras.targetNames`; o pipeline depende disso.
- **Modo de blend shape**: `BLEND_SHAPE_MODE_RELATIVE`, não `NORMALIZED`, para evitar
  artefatos de sombreamento com sliders combinados.
- **Mesh por instância**: o recurso de mesh é local por personagem, e
  `BakeMeshFromCurrentBlendShapeMix` roda no commit da edição.
- **Retargeting**: Rename Bones ON + Overwrite Axis OFF + Fix Silhouette OFF para fontes em
  T-pose. Com Rename Bones ON, `FindBone`/`SetBoneRest` resolvem pelos nomes do perfil Godot
  (`Hips`, `UpperArm`…); Overwrite Axis ON descarta o rest do Blender e conflita com a escala de
  rest em runtime.
- **Restrição de patente**: sem manipulação direta do corpo (arrastar a malha), pelo mesmo
  raciocínio que evita menus em pizza (patentes da EA).
- **Glossário**: `CharacterApplier`, `FindBlendShapeByName`, `FindBone` e `SetBoneRest` são
  nomes canônicos entre arte e código.

**Ressalva de numeração**: a auditoria de julho diz que D15–D22 "conferem com o código
inspecionado", mas não existe código de CAS. Ou a conferência foi vazia para estes números, ou o
bloco CAS foi numerado depois, sobre números que antes tinham outro conteúdo. As fontes não
permitem decidir.

## D23–D26 — Modo construção · Bloco reconstruído

Já faltavam no log em julho (`AUD` V7). O conteúdo vem do código e dos commits.

- **Núcleo puro**: `src/BuildSystem.Core` (zero Godot) com `GridCoord`, `PlacementRotation`,
  `Footprint` e `PlacementGrid`; a integração Godot fica fora da solução .NET.
  Fonte: commit `b9b8546`.
- **Fonte de verdade da ocupação**: `PlacementGrid` guarda a ocupação de **objetos**; o piso mora
  no `TileMapLayer`. `Occupy` lança se `CanPlace` for falso (fail-fast).
  Fonte: `src/BuildSystem.Core/PlacementGrid.cs:3-5,18`.
- **Footprint**: conjunto de células relativas (não um W×H), projetado numa origem e numa de 4
  rotações. Fonte: `src/BuildSystem.Core/Footprint.cs:3-7`.
- **Controle do jogador**: entrar no modo construção congela o jogador por um único ponto,
  `Player.ControlEnabled`; no mundo 3D, o `GameManager` é o único dono desse campo.
  Fontes: `godot/src/BuildController.cs:99`; `docs/integracao-mundo-design.md`.
- **No 3D**: `BuildController3D` reutiliza `PlacementGrid`/`Footprint` com células de 1 m no XZ.
  Fonte: `docs/integracao-mundo-design.md`.
- **Glossário**: `GridCoord` e `PlacementGrid` são nomes canônicos (`REG`).

Só no branch não mergeado `claude/cell-construction-mode-cjmskr` (jul/2026), portanto **não
vigente** até o merge:

- Objetos 2D isométricos trocam entre 4 texturas direcionais em vez de rotacionar o sprite, com
  Y-sort no mundo e piso em `z_index -1`.
- Viewport base 1920×1080, `canvas_items` + `expand`; células de 128×64.

## D27 — Copiar, não compartilhar · Reconstruída

Não há biblioteca compartilhada viva entre projetos: um núcleo é **copiado** para o projeto que o
usa, e a cópia pode divergir. A decisão lista a partição entre motor genérico (copiável) e camada
temática Sims (não copiável).

- Fontes: `AUD` V2; `ADD` A1; `REG`.
- Motor genérico (`AUD` V2): `Relationship`, `RelationshipMatrix`, `RelationshipValue`,
  `RelationshipModifier`, `RelationshipDecaySystem`, `Sentiment`, `InteractionDefinition`,
  `Interactions/InteractionLibrary`, `WantsAndFearsSystem`, `AspirationMeter`, `Desire`,
  `RelationshipDesires`.
- Camada temática (não porta): `CharacterTraits`, `ZodiacCompatibilityTable`,
  `AttractionCalculator`, `InterestCalculator`, `CharacterRegistry`.
- Estado das duas divergências apontadas em julho:
  - `InteractionResolver` chamava `InterestCalculator` diretamente → resolvido no commit
    "InteractionResolver recebe o bônus de tópico como delegate".
  - `AttractionWeights` misturado aos limiares → já separado em `RelationshipThresholds.cs:44-52`.
- A primeira cópia é a do motor de relacionamento para o SoccerDreamGame: ADR-0020 daquele
  repositório.

## D28 — Formato de save · Reconstruída (pendente)

O formato de persistência do mundo é uma decisão em aberto.

- Fontes: `AUD` V6 ("a futura D28"); `ADD` A4.
- Estado: **pendente**. Hoje só o mercado persiste (`MarketStateSerializer`, JSON em
  `user://market_state.json`). Relacionamentos, aspiração, carreiras, fundos, relógio e objetos
  colocados são voláteis.
- No SoccerDreamGame a questão já foi resolvida com SQLite, que vale para a cópia de lá
  (ADR-0020 §8). A decisão daqui continua aberta.

## D29–D30 — Lacuna

Nenhuma fonte cita estes números.

## D31–D34 — Guarda-roupa (Wardrobe) · Bloco reconstruído

Conteúdo do bloco (fonte: `REG`). Não há código de guarda-roupa no repositório.

- **Três camadas de atributos por peça**: estrutural (slot, camada, térmico) → descritiva
  (material, cor, corte) → avaliativa (formalidade, atratividade, tendência).
- **Atributos avaliativos não ficam no asset**: são derivados em runtime (por exemplo, de um
  `TrendVector` global). Só os atributos estruturais e descritivos são autorados por peça.
- **Cor em espaço LCH/HSL**, para regras de paleta aritméticas.
- **Glossário**: `WardrobeSelector` é nome canônico.

---

## Decisões não numeradas já no repositório

Continuam valendo onde estão; este registro não as duplica.

- `README.md`, seção "Decisões de implementação": fail-fast nos `init` de `CharacterTraits`,
  modificadores clonados por relacionamento, `IsFurious` derivado, score efetivo sem
  modificadores expirados, integração Godot fora do núcleo.
- `docs/integracao-mundo-design.md`, seção "Decisões": uma fonte de verdade para o dia
  (`SimulationCalendar`), indisponível ≠ exceção (`InteractionOutcome`), `GameManager` por
  último na cena.
- `docs/tracos-carreira-design.md`, seção "Decisões em aberto".
