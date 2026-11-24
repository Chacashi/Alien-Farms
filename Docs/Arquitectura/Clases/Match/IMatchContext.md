---
modulo: Match
namespace: AlienFarms.Match
tipo: interface
archivo: "Assets/Scripts/Match/IMatchContext.cs"
tags: [clase, modulo/match]
---
# IMatchContext

> What match states may read and do. Implemented by MatchManager (and by test fakes).

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/IMatchContext.cs`

## Derivados / implementaciones
[[MatchManager]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IMatchContext {
    <<interface>>
    +MatchSettings Settings
    +int JoinedPlayerCount
    +MatchTimer Timer
    +MatchScore Score
    +bool ForfeitOnPlayerMissing
    +PlayerId ForfeitWinner
    +IsPlayerPresent(PlayerId id) bool
    +EndByForfeit(PlayerId winner) void
    +ChangeState(MatchPhase next) void
    +ResetRound(bool respawnPlayers) void
    +SetPlayerControlsEnabled(bool enabled) void
  }
  class MatchScore:::external
  class MatchSettings:::external
  <<ScriptableObject>> MatchSettings
  class MatchTimer:::external
  IMatchContext --> MatchScore
  IMatchContext --> MatchSettings
  IMatchContext --> MatchTimer
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchPhase]], [[MatchScore]], [[MatchSettings]], [[MatchTimer]], [[PlayerId]]

## Usado por
[[CountdownState]], [[MatchManager]], [[MatchState]], [[MatchStateMachine]], [[PlayingState]], [[ResultsState]], [[WaitingForPlayersState]]
