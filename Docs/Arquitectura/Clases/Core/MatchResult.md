---
modulo: Core
namespace: AlienFarms.Core
tipo: struct
archivo: "Assets/Scripts/Core/MatchResult.cs"
tags: [clase, modulo/core]
---
# MatchResult

> Final outcome of a round. `Winner` is `PlayerId.None` on a draw. On a `MatchEndReason.Forfeit` the winner is the player who stayed, not the score leader.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/MatchResult.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchResult {
    <<struct>>
    +PlayerId Winner
    +int Player1Score
    +int Player2Score
    +MatchEndReason EndReason
    +bool IsDraw
    +bool IsForfeit
    +PlayerId Forfeiter
    +GetScore(PlayerId id) int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchEndReason]], [[PlayerId]]

## Usado por
[[GameEvents]], [[MatchResultRules]], [[MatchResultsView]], [[MatchScore]], [[MatchStatsTally]], [[OnlineMatchStatsRecorder]], [[PlayerStatsRules]], [[ResultsMenuController]], [[ResultsState]], [[ScoreBoard]]
