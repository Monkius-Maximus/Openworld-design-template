# Lista de assets — Etapa 1 de produção (SoccerDreamGame)

> Insumo de produção para `Monkius-Maximus/SoccerDreamGame` @ `b6947f3`.
> Cada dimensão abaixo deriva do que o código já fixa (citado com arquivo:linha)
> ou de uma proposta de resolução justificada na §2. Nada aqui foi implementado.

## 1. Ponto de partida real (o que existe hoje)

**O projeto tem zero assets de arte.** O único arquivo gráfico é `game/icon.svg`;
não há pasta de texturas, sprites, fontes ou áudio em lugar nenhum do repositório.

Três fatos do código mudam a prioridade da lista:

1. **Não existe resolução definida.** `game/project.godot` tem apenas as seções
   `[application]`, `[dotnet]`, `[autoload]` e `[rendering]` — **não há seção
   `[display]`**. O jogo roda hoje no default do Godot 4 (1152×648, sem stretch,
   sem escala inteira). A §2 propõe o que colocar lá; é pré-requisito de
   qualquer arte, porque define o tamanho em pixels de tudo.
2. **A partida ainda não é gráfica.** `MatchScene` é um ticker de texto — placar,
   relógio, feed rolante e box score em `Label`/`ScrollContainer`/`Button`
   (`game/scenes/match/MatchScene.cs:30-35`); o comentário em `:11` diz que o
   motor de sprites side-on "virá depois". Logo, **a Etapa 1 é dominada por
   fonte, UI e identidade — não por sprites de jogador.**
3. **O life-sim é um stub** de 13 linhas (`game/scenes/lifesim/LifeSimScene.cs:9-13`),
   com projeção isométrica e casa em grade apenas prometidas em `:6-7`.

O renderizador é `gl_compatibility`, descrito no próprio arquivo como escolha
para "a 2D / 16-bit-styled game" (`game/project.godot:31`) — a direção de arte
implícita é **pixel art**, e é sobre ela que toda a lista está dimensionada.

## 2. Resolução de projeto (proposta) — a decisão que destrava a arte

**Proposta: resolução de autoria 640×360, com escala inteira.**

640×360 é o único valor 16:9 pequeno o bastante para pixel art que cai em
**múltiplo inteiro exato** nas resoluções que dominam o mercado:

| Resolução do jogador | Fator | Encaixe |
|---|---|---|
| 1280×720 | ×2 | exato |
| 1920×1080 | ×3 | exato |
| 2560×1440 | ×4 | exato |
| 3840×2160 | ×6 | exato |
| 2560×1080 (21:9) | ×3 | exato na altura; sobra largura → mostra mais campo |
| 1366×768 | ×2 | 1280×720 com barras finas (única exceção relevante) |

Escala inteira é o que impede o "pixel tremido" (linhas de 1 px virando 1,5 px)
— o defeito visual mais comum em jogos pixel art mal configurados. Configuração
correspondente a pedir ao programador (não é asset, mas é a régua de todos eles):
stretch mode `canvas_items`, aspect `expand`, scale mode `integer`.

**Escala mundo↔pixel para a partida:** o núcleo guarda metros reais em `Vec2`
(`src/SoccerSim.Core/Pitch/Vec2.cs:11`) e o campo é o padrão FIFA
105 × 68 m, gol de 7,32 m (`src/SoccerSim.Core/Pitch/PitchGeometry.cs:41`).
Adotando **16 px/m**:

- Campo inteiro = **1680 × 1088 px** de mundo (nunca uma textura única — ver §4).
- Boca do gol 7,32 m = **117 px**.
- Jogador de 1,80 m ≈ 29 px → **célula de sprite 32×32 px**.
- A câmera a 640×360 enquadra **40 × 22,5 m** de campo, seguindo a bola.

Na visão side-on o eixo da largura do campo (Y) deve ser comprimido em ~0,55
para dar profundidade. Isso é transformação da camada de render (o núcleo
continua em metros verdadeiros), mas **os sprites precisam ser desenhados já
considerando essa compressão** — é uma instrução para o artista, não um cálculo
em runtime.

## 3. Alvo de hardware

⚠️ **Não consegui ler a página que você mandou**: a Steam Hardware Survey
devolveu **HTTP 403** para acesso automatizado (duas tentativas). Os números
abaixo são de conhecimento prévio, em **ordem de grandeza**, e devem ser
conferidos por você na fonte antes de virarem decisão:

- **1920×1080 é maioria absoluta** (~55–60% dos jogadores) — é a resolução-alvo
  primária, e o motivo de 640×360 ×3 ser a régua.
- **2560×1440** em segundo (~20%) e crescendo; **3840×2160** ainda pequeno (~4–6%).
- **1366×768** (notebooks antigos) ainda aparece (~4%) — é o piso que justifica
  não desenhar nada essencial fora de uma área segura de 1280×720 lógicos.
- **16:9 domina** (~85%+); 21:9 e 16:10 são minoria, mas o `expand` já os cobre.

Conclusão prática: **autorar uma vez a 640×360 e escalar por inteiro atende de
1366×768 a 4K sem retrabalho de arte.** Não há necessidade de assets @2x/@3x
separados — o oposto do que se faz em UI web/mobile.

## 4. Bloco A — Bloqueantes (a build atual já consome)

Estes entram primeiro porque as cenas que os usam **já existem e rodam**.

| # | Elemento | Mídia / formato | Dimensão | Qtd. | Observação |
|---|---|---|---|---|---|
| A1 | Fonte de UI | `.ttf` pixel (OFL/CC0) | corpo de 8 px, usada em 8/16/24 | 1 | Precisa de acentuação PT-BR completa (ãõçéê) e `–`/`—`. Filtro OFF na importação. |
| A2 | Fonte de placar | `.ttf` ou BMFont `.png`+`.fnt` | dígitos 16 px, **tabular** (largura fixa) | 1 | Largura fixa evita o placar "pular" ao trocar 1→2 em `MatchScene.cs:30`. |
| A3 | Escudos dos clubes | PNG-24 + alfa | 3 tiers hand-authored: 16×16, 32×32, 64×64 | 6 clubes × 3 = **18** | Os 6 times já estão no seed (`sql/9999_seed_dev.sql:21-27`: Riverside FC, Hilltop United, Costa Real, Atletico Sur, North Rovers, Lakeside Town). Não reduzir por software — pixel art precisa de cada tier redesenhado. |
| A4 | Painel 9-slice | PNG-24 + alfa | 48×48 (bordas de 8 px) | 3 variantes (escuro/claro/destaque) | Base de todo diálogo, feed e box score. |
| A5 | Botão 9-slice | PNG-24 + alfa | 32×16 (bordas de 4 px) | 3 estados (normal/hover/pressed) | `MatchScene` já tem `Button` (`:35`); `MainMenu` é `Control` inteiro. |
| A6 | Ícones de UI | PNG-24 + alfa | 16×16 | ~14 | Calendário, partida, casa/life-sim, dinheiro, contrato, treino, config, voltar, play, pause, avanço rápido (×2/×4), gol, cartão. |
| A7 | Ícones de evento de partida | PNG-24 + alfa | 16×16 | 5 | Um por `MatchEventKind`: KickOff, Chance, Goal, HalfTime, FullTime (`src/SoccerSim.Core/Simulation/MatchEngine.cs:9-13`). Hoje o feed usa o emoji ⚽ literal (`MatchScene.cs:103`) — substituir. |
| A8 | Logo do jogo | PNG-24 + alfa **e** `.svg` mestre | 320×120 (na tela) + SVG vetorial | 1 | O SVG é só o mestre de autoria; o jogo consome o PNG. |
| A9 | Fundo do menu principal | PNG-24 | 640×360 | 1 | Cena existe (`game/scenes/main_menu/MainMenu.tscn`). |
| A10 | Ícone do aplicativo | `.png` 1024×1024 + `.ico` multi-res | ico com 16/32/48/64/128/256 | 1 | Substitui o `game/icon.svg` placeholder. |
| A11 | SFX de UI | `.wav` PCM 16-bit 44.1 kHz | 0,08–0,20 s cada | 4 | hover, click, voltar, negado. WAV (não OGG) por serem curtos — latência zero. |
| A12 | SFX de partida (ticker) | `.wav` (curtos) / `.ogg` (loop) | apito 1 s ×3; gol 4 s; "ooh" 2 s; torcida loop 45 s | 6 | Torcida em `.ogg` estéreo com loop costurado. Já dá vida ao ticker atual, antes de qualquer sprite. |
| A13 | Música de menu | `.ogg` Vorbis ~160 kbps | 60–120 s em loop | 1 | Um tema só na Etapa 1. |

**Total do Bloco A: ~60 arquivos** — é uma etapa realista para um artista em
poucas semanas, e transforma a build atual (texto puro) em algo apresentável.

## 5. Bloco B — Motor side-on da partida (próxima fatia, não Etapa 1)

Só faz sentido quando o código de `MatchScene.cs:11` sair do "will later live".
Especificado aqui para orçamento, não para produzir agora.

| # | Elemento | Formato | Dimensão | Observação |
|---|---|---|---|---|
| B1 | Sprite sheet do jogador de linha | PNG-24 + alfa | célula 32×32; folha 256×256 | **Autorar em tons de cinza com máscara de paleta**: 1 folha serve todos os clubes por troca de paleta em shader (2 cores de uniforme + pele + cabelo). Sem isso, 6 clubes × 2 uniformes = 12 folhas e a conta explode a cada liga nova. |
| B2 | Animações do jogador | — | idle 4, corrida 8, passe 4, chute 5, carrinho 5, queda 4, comemoração 6 | 3 facings autorados (lado, diagonal-cima, diagonal-baixo) + espelhamento = 6 direções. |
| B3 | Sprite sheet do goleiro | PNG-24 + alfa | célula 32×32; folha 192×160 | idle 4, mergulho E/D 5 cada, defesa 3, reposição 4. |
| B4 | Bola | PNG-24 + alfa | célula 8×8, 4 frames de giro | 0,22 m ≈ 3,5 px reais — arredondar para 8×8 com folga e sombra separada. |
| B5 | Sombra | PNG-24 + alfa | 16×8 | Uma só, escalada por altura. |
| B6 | Grama | PNG-24, **seamless** | tile 32×32, 2 variantes | As duas variantes alternadas fazem o listrado do corte. Nunca uma textura de 1680×1088. |
| B7 | Marcações do campo | PNG-24 + alfa | peças de 32×32 + círculo central 192×192 | Linhas como sprites, não pintadas na grama — permite reuso e campo de outras dimensões. |
| B8 | Gol + rede | PNG-24 + alfa | 128×80 | 7,32 m = 117 px na escala de 16 px/m. |
| B9 | Arquibancada / público | PNG-24 | faixa tileável 128×64, 3 densidades | Fundo estático; multidão animada fica para depois. |
| B10 | SFX de campo | `.wav` | chute ×4, passe ×3, cabeceio ×2, trave, rede, carrinho (0,2–0,6 s) | |

## 6. Bloco C — Life-sim isométrico (depende de decisão em aberto)

O life-sim é um stub e — conforme o adendo da auditoria — ainda **não foi
decidido** se ele herdará os núcleos do Openworld ou seguirá com primitivas
próprias. Produzir arte iso agora é risco de retrabalho. Fica o esqueleto:

| # | Elemento | Formato | Dimensão | Observação |
|---|---|---|---|---|
| C1 | Tile de piso iso | PNG-24 + alfa | 32×16 (proporção 2:1) | 2:1 é o padrão iso que casa com escala inteira. |
| C2 | Objetos de casa | PNG-24 + alfa | 32×32 e 64×48 | Só **3 itens** existem no seed (`sql/9999_seed_dev.sql:69-71`): Basic Bed, Orthopedic Bed, Home Gym. |
| C3 | Personagem iso | PNG-24 + alfa | célula 32×48 | 4 direções + idle/andar. Mesma máscara de paleta do B1. |
| C4 | Retratos de diálogo | PNG-24 + alfa | 64×64 | Necessários para os eventos Tier **Medium** (diálogo com escolha) e **High** (mini-jogo) de `src/SoccerSim.Core/Events/EventTypes.cs:6-13`. Tier Low é notificação — só ícone. |

## 7. Regras de formato e importação (valem para tudo)

- **PNG-24 com alfa** para todo sprite e UI. **Nunca JPEG** (artefato em pixel
  art e sem canal alfa). PNG-8 indexado só se a paleta for travada.
- **Importação no Godot**: `Filter = Off`, `Mipmaps = Off`, `Fix Alpha Border = On`.
  Filtro ligado é o que borra pixel art — é o erro nº 1 nesse tipo de projeto.
- **Áudio**: `.wav` PCM 16-bit 44,1 kHz para efeitos curtos (< 3 s); `.ogg`
  Vorbis para loops e música. Godot importa ambos nativamente; **evitar MP3**
  (patente/licença resolvidas, mas o loop nunca fecha sem gap).
- **Vídeo: nenhum na Etapa 1.** O suporte a vídeo do Godot 4 é limitado a Theora
  (`.ogv`), com qualidade/peso ruins. Se houver abertura ou cutscene, fazer como
  sequência de sprites ou animação em cena — não como arquivo de vídeo.
- **Fontes**: `.ttf`/`.otf` com licença **OFL, CC0 ou comprada para uso
  comercial**. Verificar antes de importar; fonte com licença errada é o passivo
  jurídico mais comum em jogo indie.
- **Paleta**: fixar uma paleta mestre (sugestão: 32–48 cores) num `.gpl`/`.ase`
  e todo asset sair dela. É o que faz 60 arquivos de artistas diferentes
  parecerem um jogo só.

**Estrutura de pastas sugerida** (dentro de `game/`, que é o único projeto Godot):

```
game/assets/
  fonts/        ui.ttf, scoreboard.ttf
  ui/           panel_*.png, button_*.png, icons/*.png
  branding/     logo.png, icon_1024.png, icon.ico
  clubs/        crest_<slug>_{16,32,64}.png
  match/        players/, ball/, pitch/, goal/
  lifesim/      tiles/, objects/, portraits/
  audio/sfx/    *.wav
  audio/music/  *.ogg
```

**Nomenclatura**: `snake_case`, sem acento, sem espaço, com o tamanho no fim
quando houver tiers (`crest_riverside_32.png`). Sheets terminam em `_sheet` e
vêm acompanhados de um `.json`/`.tres` com o mapa de frames.

## 8. O que NÃO produzir agora (e por quê)

- **Uniformes por clube** — a máscara de paleta do B1 torna isso dado (duas
  cores por clube), não asset. Produzir 12 folhas de uniforme seria jogar
  trabalho fora.
- **Arte do life-sim iso** — decisão de arquitetura em aberto (ver adendo da
  auditoria); risco alto de retrabalho.
- **Qualquer asset @2x/@3x** — a escala inteira da §2 já resolve 1366×768 → 4K
  com um único conjunto.
- **Vídeo, telas de carregamento elaboradas, retratos de todos os jogadores** —
  o banco tem ~22 jogadores no seed, mas uma carreira real terá milhares;
  retrato individual só faz sentido como composição procedural (camadas), que é
  decisão de outra etapa.
