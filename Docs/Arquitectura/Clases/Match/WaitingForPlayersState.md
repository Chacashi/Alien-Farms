---
modulo: Match
namespace: AlienFarms.Match
tipo: class
archivo: "Assets/Scripts/Match/WaitingForPlayersState.cs"
tags: [clase, modulo/match]
---
# WaitingForPlayersState

> Free roam until both players have joined; captures are not scored (the countdown resets the round).

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/WaitingForPlayersState.cs`

## Hereda / implementa
[[MatchState]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class WaitingForPlayersState {
    +MatchPhase Phase
    +Enter() void
    +Tick(float deltaTime) void
  }
  class MatchState:::external
  <<abstract>> MatchState
  WaitingForPlayersState --|> MatchState
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IMatchContext]], [[MatchPhase]], [[MatchState]]

## Usado por
[[MatchStateMachine]]
