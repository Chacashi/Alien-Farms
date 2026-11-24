---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: static
archivo: "Assets/Scripts/Networking/Persistence/PlayerStatsRules.cs"
tags: [clase, modulo/networking]
---
# PlayerStatsRules

> Pure accumulation rules of the lifetime stats. Saturating, never negative, no allocation.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PlayerStatsRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerStatsRules {
    <<static>>
    +ForMatch(PlayerId local, MatchResult result, int animals, int tackles)$ PlayerStatsSummary
    +Win(int animals, int tackles)$ PlayerStatsSummary
    +Draw(int animals, int tackles)$ PlayerStatsSummary
    +Loss(int animals, int tackles)$ PlayerStatsSummary
    +Add(PlayerStatsSummary a, PlayerStatsSummary b)$ PlayerStatsSummary
    +Subtract(PlayerStatsSummary a, PlayerStatsSummary b)$ PlayerStatsSummary
    +Sanitize(PlayerStatsSummary stats)$ PlayerStatsSummary
    +IsZero(PlayerStatsSummary stats)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchResult]], [[PlayerId]], [[PlayerStatsSummary]]

## Usado por
[[MatchStatsTally]], [[PendingStats]], [[PendingStatsCodec]], [[PlayerStatsCodec]], [[PlayerStatsLedger]]
