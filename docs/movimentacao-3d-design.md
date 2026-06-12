# Movimentação 3D estilo Project Zomboid

Sistema de movimento de personagem + câmera isométrica inspirado em
*Project Zomboid* (PZ). O PZ é escrito em Java (LWJGL); aqui tudo foi
adaptado para a stack do projeto: **C# / .NET 8**, com a matemática num
núcleo puro e testável (`src/MovementSystem.Core`) e a integração com a
engine em **Godot 4.6** (`godot/src/movement/`).

A técnica de mira segue a recomendação da discussão
["How to create an aim system like Project Zomboid"](https://discussions.unity.com/t/how-to-create-an-aim-system-like-project-zomboid/946759/9)
(Unity Discussions): o cursor do mouse vira um raio da câmera e é
intersectado com um **plano horizontal matemático na altura do
personagem** — e não com um raycast físico contra o cenário. Isso evita
que a mira "pule" quando o cursor passa por cima de uma caixa, parede ou
NPC. A mesma ideia foi portada de Unity (`Plane.Raycast`) para o nosso
núcleo (`AimCalculator.TryIntersectPlane`), de modo que funciona em
qualquer engine.

## O que foi reproduzido do PZ

| Mecânica do PZ                         | Implementação aqui                                        |
|----------------------------------------|-----------------------------------------------------------|
| Andar/correr/sprint/agachar            | `MovementStance` + `StanceResolver` (Shift/Alt/C)         |
| WASD relativo à tela                   | `MovementCalculator.DirectionFromInput` (gira pelo yaw da câmera) |
| Mirar com botão direito                | `aim` (RMB): personagem gira para encarar o cursor        |
| Mirando, não se corre                  | `StanceResolver.Resolve` limita a Walk (`AimWalkSpeed`)   |
| Recuar mirando é lento                 | `DirectionalSpeedMultiplier` (frente 1.0 / lado 0.8 / costas 0.5) |
| Câmera segue o personagem              | `CameraRigCalculator.SmoothFollow` (suavização exponencial) |
| Câmera desliza rumo ao cursor ao mirar | `CameraRigCalculator.AimAnchor` (look-ahead com clamp)    |
| Zoom por scroll                        | `ApplyZoomSteps` na câmera **ortográfica** (visual iso do PZ) |
| — (extra; o PZ tem ângulo fixo)        | Q/E giram a vista em passos de 45°                        |

## Arquitetura

```
src/MovementSystem.Core/            # matemática pura (System.Numerics), sem Godot
├── MovementThresholds.cs           # todas as constantes de tuning num só lugar
├── MovementStance.cs               # posturas + resolução de prioridade + velocidade
├── AimCalculator.cs                # raio×plano, yaw, wrap e rotação por caminho curto
├── MovementCalculator.cs           # input→direção de mundo, penalidade direcional, aceleração
└── CameraRigCalculator.cs          # follow suave, look-ahead da mira, zoom em passos
tests/MovementSystem.Tests/         # xUnit (roda no CI junto com o resto)

godot/src/movement/                 # camada fina de engine (só I/O)
├── PlayerController3D.cs           # CharacterBody3D: input, gravidade, rotação, MoveAndSlide
└── IsoCameraRig.cs                 # Node3D: follow, zoom, rotação em 45°, look-ahead
godot/scenes/World3D.tscn           # cena de teste: chão, obstáculos, player-cápsula, rig
```

Convenções (iguais às do Godot): **Y para cima**, chão no plano XZ,
"frente" do personagem = **-Z** quando `Rotation.Y = 0`. O yaw devolvido
pelo núcleo pode ser atribuído direto em `Rotation.Y`.

A cena monta o rig em três nós para manter os ângulos independentes:
`CameraRig` (só **yaw**, é o que o player consulta) → `Pivot` (só
**pitch**, -40°) → `Camera3D` (ortográfica, a 30 m no eixo Z local).

## Controles

| Tecla / botão     | Ação                                                  |
|-------------------|--------------------------------------------------------|
| **W A S D**       | Mover (relativo à câmera)                              |
| **Shift**         | Correr                                                 |
| **Alt**           | Sprint                                                 |
| **C** (segurar)   | Sneak (anda devagar; prioridade sobre tudo)            |
| **Botão direito** | Mirar: encara o cursor, anda devagar, recuar é mais lento |
| **Scroll**        | Zoom in/out (com limites)                              |
| **Q / E**         | Girar a câmera em passos de 45°                        |

## Passo a passo para testar

1. Abra a pasta `godot/` no **Godot 4.6 (versão .NET/Mono)**. Na primeira
   abertura, deixe a engine importar os recursos.
2. Compile o C#: **Build** no canto superior direito (ou `dotnet build`
   dentro de `godot/`). Precisa do SDK do .NET 8.
3. No FileSystem, abra `res://scenes/World3D.tscn` e rode a **cena
   atual** com **F6** (a cena principal do projeto continua sendo o modo
   de construção 2D; para tornar o 3D a cena principal:
   *Project Settings → Application → Run → Main Scene*).
4. Verifique, nesta ordem:
   - **Movimento:** WASD desloca a cápsula; o "nariz" amarelo aponta para
     onde ela anda. Shift/Alt/C mudam a velocidade visivelmente.
   - **Colisão:** ande contra as caixas cinzas — o personagem desliza ao
     longo delas (MoveAndSlide), sem atravessar.
   - **Mira:** segure o **botão direito** e mova o mouse ao redor — o
     disco vermelho marca o ponto de mira e o personagem gira para
     encará-lo mesmo parado. Passe o cursor **por cima de uma caixa**: o
     disco não deve "subir" nem pular (é o plano matemático funcionando).
   - **Penalidade direcional:** mirando, ande para trás (S com o cursor
     acima do personagem) — nitidamente mais lento que andar para frente.
   - **Câmera:** ela segue com um leve atraso suave; ao mirar longe, o
     enquadramento desliza um pouco rumo ao cursor (limitado a 4 m);
     scroll aproxima/afasta dentro dos limites; Q/E giram 45° e o WASD
     continua coerente com a tela após o giro.
5. Os testes do núcleo rodam com `dotnet test` na raiz do repositório
   (CI já cobre: `MovementSystem.Tests`, 34 testes).

## Aplicando os assets finais

Quando o modelo/animações chegarem, a cápsula é só placeholder:

1. Em `World3D.tscn`, troque os filhos visuais do nó `Player`
   (`MeshInstance3D` e `Nose`) pela cena do modelo (glTF/GLB
   instanciado). **Mantenha** o `CollisionShape3D` (ajuste raio/altura à
   silhueta do modelo) e o script `PlayerController3D` no nó raiz.
2. O modelo deve olhar para **-Z** no espaço local (convenção do Godot).
   Se o asset vier virado para +Z, gire o nó visual 180° em Y — não mude
   o script.
3. Ajuste `AimPlaneHeight` (export do player) para a **altura do
   peito/arma** do modelo — é o ponto fino da técnica da discussão da
   Unity: o plano de mira na altura da arma deixa o cursor "colado" no
   alvo em qualquer zoom.
4. Animações: o controller já expõe o que um `AnimationTree` precisa —
   `Velocity` (blend idle/walk/run por velocidade), `IsAiming` (pose de
   arma erguida) e a postura pode ser derivada das mesmas ações de input.
5. Tuning fino (velocidades, penalidades, zoom, look-ahead) é todo feito
   em `MovementThresholds.cs`, num lugar só.

## Adaptação Java → C#

O PZ não tem código-fonte aberto; a adaptação foi conceitual: o loop
Java do PZ (atualização por tick, mira por projeção de tela→mundo,
posturas com prioridade) virou métodos estáticos puros sobre
`System.Numerics`, com o mesmo espírito *data-driven* dos outros módulos
do repositório (thresholds centralizados, resolvers estáticos, camada de
engine fina). Nada depende de Godot no núcleo — os mesmos cálculos
serviriam em Unity ou MonoGame.
