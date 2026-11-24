---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: class
archivo: "Assets/Scripts/Networking/Persistence/SimulatedFailureStatsStore.cs"
tags: [clase, modulo/networking]
---
# SimulatedFailureStatsStore

> Decorator that makes every call fail as offline (retryable) without touching the network while shouldFail is true (read on every call, so it can be toggled at runtime). Driven by NetworkSettings.SimulateStatsFailure, which is always false outside the Editor and development builds.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/SimulatedFailureStatsStore.cs`

## Hereda / implementa
[[IPlayerStatsStore]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class SimulatedFailureStatsStore {
    +LoadOwnAsync() Task~StatsStoreResult~
    +LoadPublicAsync(string playerId) Task~StatsStoreResult~
    +SaveOwnAsync(PlayerStatsRecord record, string writeLock) Task~StatsStoreResult~
  }
  class IPlayerStatsStore:::external
  <<interface>> IPlayerStatsStore
  SimulatedFailureStatsStore ..|> IPlayerStatsStore
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IPlayerStatsStore]], [[PlayerProgressErrorKeys]], [[PlayerStatsRecord]], [[StatsStoreResult]]

## Usado por
[[CloudSaveStatsService]]
