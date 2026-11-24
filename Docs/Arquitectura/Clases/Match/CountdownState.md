---
modulo: Match
namespace: AlienFarms.Match
tipo: class
archivo: "Assets/Scripts/Match/CountdownState.cs"
tags: [clase, modulo/match]
---
# CountdownState

> Resets the round, freezes players and counts down before play starts.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/CountdownState.cs`

## Hereda / implementa
[[MatchState]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class CountdownState {
    +MatchPhase Phase
    +Enter() void
    +Tick(float deltaTime) void
  }
  class MatchState:::external
  <<abstract>> MatchState
  CountdownState --|> MatchState
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[IMatchContext]], [[MatchPhase]], [[MatchState]], [[Phase]]

## Usado por
[[MatchStateMachine]]

## Eventos que publica
[[Eventos GameEvents#OnCountdownFinished|OnCountdownFinished]], [[Eventos GameEvents#OnCountdownTick|OnCountdownTick]]
