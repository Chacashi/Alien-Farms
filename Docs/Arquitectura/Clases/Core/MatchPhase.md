---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/MatchPhase.cs"
tags: [clase, modulo/core]
---
# MatchPhase

> Phases of a match, driven by the MatchManager state machine.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/MatchPhase.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchPhase {
    <<enumeration>>
    None
    WaitingForPlayers
    Countdown
    Playing
    Results
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`None`, `WaitingForPlayers`, `Countdown`, `Playing`, `Results`

## Usado por
[[BombSpawner]], [[CaptureRules]], [[CountdownState]], [[CountdownView]], [[GameEvents]], [[IMatchContext]], [[MatchManager]], [[MatchResultsView]], [[MatchState]], [[MatchStateMachine]], [[PauseHoldIndicatorView]], [[PauseRules]], [[PlayingState]], [[ResultsMenuController]], [[ResultsState]], [[WaitingForPlayersState]]
