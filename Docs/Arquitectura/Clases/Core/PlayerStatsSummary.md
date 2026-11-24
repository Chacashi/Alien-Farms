---
modulo: Core
namespace: AlienFarms.Core
tipo: struct
archivo: "Assets/Scripts/Core/PlayerStatsSummary.cs"
tags: [clase, modulo/core]
---
# PlayerStatsSummary

> Lifetime stats of the local player shown on the title screen.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/PlayerStatsSummary.cs`

## Hereda / implementa
`IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerStatsSummary {
    <<struct>>
    +int MatchesPlayed
    +int Wins
    +int Draws
    +int Losses
    +int AnimalsScored
    +int TacklesLanded
    +Equals(PlayerStatsSummary other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[MatchStatsTally]], [[OnlineMatchStatsRecorder]], [[PendingStats]], [[PendingStatsCodec]], [[PlayerProgressChannel]], [[PlayerStatsCodec]], [[PlayerStatsLedger]], [[PlayerStatsRecord]], [[PlayerStatsRules]], [[PlayerStatsSync]], [[RivalStatsView]], [[TitleStatsView]]
