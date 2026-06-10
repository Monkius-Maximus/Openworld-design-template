# Protótipo Godot da etapa de batalhas

Este diretório entrega a integração com engine que estava **planejada e nunca
entregue** ("Parte 8 da spec", omitida do build até aqui) — já na **Godot**,
não mais na Unreal. É um projeto Godot 4 (.NET) que reusa
`src/BattleSystem.Core` e `src/RelationshipSystem.Core` **sem nenhuma
alteração no núcleo**.

> Este projeto **não** faz parte da `RelationshipSystem.sln` (e do CI) de
> propósito: o `Godot.NET.Sdk` só compila de verdade dentro da engine. O mesmo
> precedente do repositório: o núcleo permanece livre de dependências.

## Requisitos

- [Godot 4.3+ — versão **.NET**](https://godotengine.org/download) (gratuita, MIT)
- SDK do .NET 8

## Como rodar

1. Abra o Godot e importe esta pasta (`godot/project.godot`).
2. Na primeira abertura, a engine gera a solution própria e compila
   (`Build` no topo, ou apenas pressione ▶).
3. Rode a cena principal (`scenes/Battle.tscn`): uma batalha 2×2 com barras de
   vida, log e botão **Próximo turno** — a IA joga os dois lados.

**Zero assets**: a UI inteira é construída em código (`ProgressBar`,
`RichTextLabel`, `Button`). Não há textura, modelo ou som no projeto.

## Arquitetura (e o que mudou vindo da Unreal)

| Conceito Unreal                      | Equivalente aqui (Godot)                          |
|--------------------------------------|---------------------------------------------------|
| Actor / Pawn                          | `Node` / cena instanciável                        |
| ActorComponent                        | nós-filhos (composição por cena)                  |
| GameMode / GameState                  | autoload (singleton) ou nó controlador da cena    |
| Delegates / Event dispatchers         | eventos C# do núcleo → `[Signal]` no adaptador    |
| GameplayAbility (GAS)                 | `BattleActionDefinition` (dados + predicados puros) |
| DataAsset / DataTable                 | catálogos estáticos do núcleo (`BattleActionLibrary`); promovíveis a `Resource` |
| Tick                                  | `_Process` (não usado: o combate é por turnos)    |

O ponto central: **toda regra vive no núcleo C# testável por `dotnet test`**;
o `BattleController` só traduz eventos em sinais e desenha o estado. Trocar a
UI, plugar input do jogador no lugar da IA ou portar para outra engine não
toca uma linha de regra.

## Onde encaixar assets open source depois

Substitua a UI por cenas com arte mantendo o `BattleController` como fonte de
verdade. Sugestões com licença permissiva (detalhes em
[`../docs/batalha-design.md`](../docs/batalha-design.md)):

- [Kenney](https://kenney.nl) — sprites, UI e SFX, CC0
- [KayKit (Kay Lousberg)](https://kaylousberg.itch.io) — modelos 3D low-poly + animações, CC0
- [Quaternius](https://quaternius.com) — modelos 3D animados, CC0
- [game-icons.net](https://game-icons.net) — ícones de habilidade, CC BY 3.0
- [OpenGameArt](https://opengameart.org) — filtrar por CC0/CC-BY
