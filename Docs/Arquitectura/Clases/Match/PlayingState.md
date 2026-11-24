---
modulo: Match
namespace: AlienFarms.Match
tipo: class
archivo: "Assets/Scripts/Match/PlayingState.cs"
tags: [clase, modulo/match]
---
# PlayingState

> Round in progress: timer runs (1 Hz event) and score changes are published. A missing player either aborts the round (local) or ends it as a forfeit won by the player who stayed (`IMatchContext.ForfeitOnPlayerMissing`).

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/PlayingState.cs`

## Hereda / implementa
[[MatchState]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayingState {
    +MatchPhase Phase
    +Enter() void
    +Tick(float deltaTime) void
  }
  class MatchState:::external
  <<abstract>> MatchState
  PlayingState --|> MatchState
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[IMatchContext]], [[MatchPhase]], [[MatchResultRules]], [[MatchState]], [[MatchTimer]], [[Phase]], [[PlayerId]]

## Usado por
[[MatchStateMachine]]

## Eventos que publica
[[Eventos GameEvents#OnMatchTimeChanged|OnMatchTimeChanged]]
