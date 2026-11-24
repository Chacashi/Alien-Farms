---
modulo: Match
namespace: AlienFarms.Match
tipo: static
archivo: "Assets/Scripts/Match/MatchResultRules.cs"
tags: [clase, modulo/match]
---
# MatchResultRules

> How the published `MatchResult` is built from the scored one. Shared by the match (server / local) and the online replica, so both raise the same result.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/MatchResultRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchResultRules {
    <<static>>
    +Resolve(MatchResult scored, PlayerId forfeitWinner)$ MatchResult
    +TryGetSoleRemaining(bool player1Present, bool player2Present, PlayerId remaining)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchEndReason]], [[MatchResult]], [[PlayerId]]

## Usado por
[[NetworkMatchState]], [[PlayingState]], [[ResultsState]]
