---
modulo: Match
namespace: AlienFarms.Match
tipo: abstract
archivo: "Assets/Scripts/Match/MatchState.cs"
tags: [clase, modulo/match]
---
# MatchState

> Base of the match State pattern. Plain C# (testable in EditMode). Rule: no state calls `IMatchContext.ChangeState` from `Enter`; transitions happen only in `Tick` or `OnConfirmPressed`, so the phase event always precedes the next Enter.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/MatchState.cs`

## Derivados / implementaciones
[[CountdownState]], [[PlayingState]], [[ResultsState]], [[WaitingForPlayersState]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchState {
    <<abstract>>
    +MatchPhase Phase*
    #IMatchContext Context
    +bool IsRematchReady
    +bool IsResultsInputReady
    +Enter() void
    +Tick(float deltaTime) void
    +Exit() void
    +OnConfirmPressed(PlayerId playerId) void
    #AbortIfPlayerMissing() bool
  }
  class IMatchContext:::external
  <<interface>> IMatchContext
  MatchState --> IMatchContext
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IMatchContext]], [[MatchPhase]], [[Phase]], [[PlayerId]]

## Usado por
[[CountdownState]], [[MatchStateMachine]], [[PlayingState]], [[ResultsState]], [[WaitingForPlayersState]]
