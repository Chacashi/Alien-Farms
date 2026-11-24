---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: struct
archivo: "Assets/Scripts/Networking/Persistence/StatsStoreResult.cs"
tags: [clase, modulo/networking]
---
# StatsStoreResult

> Outcome of an `IPlayerStatsStore` call, free of SDK types. A load carries the decoded `Record` (`Read`) and its `WriteLock`; a save carries the new write lock.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/StatsStoreResult.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class StatsStoreResult {
    <<struct>>
    +StatsStoreOutcome Outcome
    +StatsReadResult Read
    +PlayerStatsRecord Record
    +string WriteLock
    +string ErrorKey
    +float RetryAfter
    +bool IsOk
    +Loaded(StatsReadResult read, PlayerStatsRecord record, string writeLock)$ StatsStoreResult
    +Saved(string writeLock)$ StatsStoreResult
    +Conflict()$ StatsStoreResult
    +Retryable(string errorKey, float retryAfter)$ StatsStoreResult
    +Failed(string errorKey)$ StatsStoreResult
  }
  class PlayerStatsRecord:::external
  <<struct>> PlayerStatsRecord
  StatsStoreResult --> PlayerStatsRecord
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerProgressErrorKeys]], [[PlayerStatsRecord]], [[StatsReadResult]], [[StatsStoreOutcome]]

## Usado por
[[CloudSavePlayerStatsStore]], [[IPlayerStatsStore]], [[PlayerStatsSync]], [[SimulatedFailureStatsStore]]
