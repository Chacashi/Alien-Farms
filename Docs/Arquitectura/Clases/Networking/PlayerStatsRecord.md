---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: struct
archivo: "Assets/Scripts/Networking/Persistence/PlayerStatsRecord.cs"
tags: [clase, modulo/networking]
---
# PlayerStatsRecord

> The persisted stats of one player (cloud key `PlayerStatsCodec.Key`): schema version, absolute totals and the id of the last batch applied (idempotence across lost responses and conflicts).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PlayerStatsRecord.cs`

## Hereda / implementa
`IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerStatsRecord {
    <<struct>>
    +PlayerStatsRecord Empty$
    +int Version
    +PlayerStatsSummary Stats
    +string LastBatchId
    +Equals(PlayerStatsRecord other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class PlayerStatsSummary:::external
  <<struct>> PlayerStatsSummary
  PlayerStatsRecord --> PlayerStatsSummary
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerStatsCodec]], [[PlayerStatsSummary]]

## Usado por
[[CloudSavePlayerStatsStore]], [[IPlayerStatsStore]], [[PlayerStatsCodec]], [[PlayerStatsLedger]], [[PlayerStatsSync]], [[SimulatedFailureStatsStore]], [[StatsStoreResult]]
