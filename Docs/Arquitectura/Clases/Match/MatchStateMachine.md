---
modulo: Match
namespace: AlienFarms.Match
tipo: class
archivo: "Assets/Scripts/Match/MatchStateMachine.cs"
tags: [clase, modulo/match]
---
# MatchStateMachine

> Plain C# match state machine: owns one state per `MatchPhase` and performs transitions. Side effects (players, animals, score, timer) live in the `IMatchContext`.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/MatchStateMachine.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchStateMachine {
    +MatchPhase CurrentPhase
    +bool IsRematchReady
    +bool IsResultsInputReady
    +ChangeState(MatchPhase next) bool
    +Tick(float deltaTime) void
    +OnConfirmPressed(PlayerId playerId) void
  }
  class IMatchContext:::external
  <<interface>> IMatchContext
  class MatchState:::external
  <<abstract>> MatchState
  MatchStateMachine --> IMatchContext
  MatchStateMachine --> MatchState
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CountdownState]], [[GameEvents]], [[IMatchContext]], [[MatchPhase]], [[MatchState]], [[Phase]], [[PlayerId]], [[PlayingState]], [[ResultsState]], [[WaitingForPlayersState]]

## Usado por
[[MatchManager]]

## Eventos que publica
[[Eventos GameEvents#OnMatchPhaseChanged|OnMatchPhaseChanged]]
