---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: interface
archivo: "Assets/Scripts/Networking/Persistence/IPlayerStatsStore.cs"
tags: [clase, modulo/networking]
---
# IPlayerStatsStore

> Strategy for the persisted stats of the signed-in player (key `PlayerStatsCodec.Key`). Implementations never throw: every failure comes back as a `StatsStoreResult`.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/IPlayerStatsStore.cs`

## Derivados / implementaciones
[[CloudSavePlayerStatsStore]], [[SimulatedFailureStatsStore]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IPlayerStatsStore {
    <<interface>>
    +LoadOwnAsync() Task~StatsStoreResult~
    +LoadPublicAsync(string playerId) Task~StatsStoreResult~
    +SaveOwnAsync(PlayerStatsRecord record, string writeLock) Task~StatsStoreResult~
  }
  class StatsStoreResult:::external
  <<struct>> StatsStoreResult
  IPlayerStatsStore --> StatsStoreResult
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerStatsRecord]], [[StatsStoreResult]]

## Usado por
[[CloudSavePlayerStatsStore]], [[CloudSaveStatsService]], [[PlayerStatsSync]], [[SimulatedFailureStatsStore]]
