---
modulo: Match
namespace: AlienFarms.Match
tipo: class
archivo: "Assets/Scripts/Match/ResultsState.cs"
tags: [clase, modulo/match]
---
# ResultsState

> Round over: publishes the result, freezes players and restarts on Confirm after a short delay.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/ResultsState.cs`

## Hereda / implementa
[[MatchState]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class ResultsState {
    +MatchPhase Phase
    +bool IsRematchReady
    +Enter() void
    +Tick(float deltaTime) void
    +OnConfirmPressed(PlayerId playerId) void
  }
  class MatchState:::external
  <<abstract>> MatchState
  ResultsState --|> MatchState
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[IMatchContext]], [[MatchPhase]], [[MatchState]], [[PlayerId]]

## Usado por
[[MatchStateMachine]]

## Eventos que publica
[[Eventos GameEvents#OnMatchEnded|OnMatchEnded]]
