# Resolução, escala e proporções

Decisão de base para dimensionar arte, UI e células. Fecha a dívida técnica de
o projeto não ter seção `[display]` — até então rodava no default da Godot
(1152×648, stretch `disabled`), o que deixava escala de UI e de mundo indefinidas.

## Dados que embasam (Steam Hardware Survey, junho/2026)

| Resolução primária | Fatia | Proporção |
|--------------------|-------|-----------|
| 1920×1080          | 51,1% | 16:9      |
| 2560×1440          | 19,9% | 16:9      |
| 3840×2160 (4K)     | 4,5%  | 16:9      |

**~75% do público está em 16:9**, e 1080p sozinho é maioria absoluta. As três
resoluções dominantes são múltiplos exatos entre si (1440p = 1080p × 1,333;
4K = 1080p × 2), então uma base 1080p sobe para todas sem matemática torta.

## Decisão

| Item | Valor | Por quê |
|------|-------|---------|
| Viewport base | **1920×1080** | Casa a resolução da maioria; 1:1 sem escala no caso mais comum |
| Janela (dev) | 1280×720 | Cabe em monitor de trabalho sem sair do 16:9 |
| `stretch/mode` | `canvas_items` | Escala UI e mundo juntos, mantendo vetores/fontes nítidos |
| `stretch/aspect` | `expand` | Em 16:10 e ultrawide mostra **mais mundo** em vez de barra preta |
| Célula (2D) | **128×64 px** (2:1) | ~15 células na largura em 1080p — leitura tipo The Sims 2 |
| Célula (3D) | **1 unidade = 1 m** | `BuildController3D` já usa célula unitária; 1 célula ≙ 1 m nos dois modos |

### Por que 1080p base e não 640×360 (pixel-perfect)

640×360 escalaria em inteiro para 1080p (×3), 1440p (×4) e 4K (×6) — ideal para
pixel art. Foi descartado por dois motivos concretos:

1. **A UI existente já é 1080p.** O `CurrencyPanel` está autorado em
   `176..640 × 16..420` — precisa de 420 px lógicos de altura. Num canvas de
   360 px ele literalmente não cabe. Rebasear significaria reescrever todo
   offset de UI já feito.
2. **Sim-game é denso em texto.** Nomes, dinheiro, barras de relacionamento,
   painel de mercado. 640×360 dá pouquíssimo espaço para esse tipo de HUD.

O custo é abrir mão de nitidez pixel-perfect: a arte deve ser **ilustrada/escalável**,
não pixel art estrita. Se a direção de arte virar pixel art, esta decisão precisa
ser revista — é o único gatilho que a invalida.

## Contrato de arte (2D)

| Asset | Dimensão | Regra |
|-------|----------|-------|
| Piso (tile) | **128×64** | Losango tocando os 4 pontos médios; **cantos transparentes** (canto opaco vaza no vizinho) |
| Objeto 1 célula | base **128** de largura | Altura livre |
| Objeto N células | base **128 × N** | Largura acompanha o footprint na direção |
| Direções | **4 texturas** | Ordem `Deg0, Deg90, Deg180, Deg270` |
| Origem do sprite | **centro da base** | `centered = false`, `offset = (-largura/2, -altura)` |

Para 4K com nitidez real, autorar a fonte em **2×** (piso 256×128) e deixar a
Godot reduzir — reduzir preserva qualidade, ampliar não.

O código lê `TileSet.TileSize` em runtime (`BuildController`, `BuildGridOverlay`),
então trocar a dimensão da célula não exige mudar C#: basta o `.tres` e a arte.

## Escala humana

Com célula de 128×64 (≙ 1 m), uma pessoa fica em torno de **40×110 px** — ocupa
menos de uma célula na base e ~1,7 célula em altura. É a referência para
dimensionar personagens e mobília.
