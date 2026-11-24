---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: class
archivo: "Assets/Scripts/Networking/Persistence/PlayerPrefsPendingStatsStore.cs"
tags: [clase, modulo/networking]
---
# PlayerPrefsPendingStatsStore

> `IPendingStatsStore` on PlayerPrefs, key stats.pending.{ugsId} (one per account, so MPPM clones sharing PlayerPrefs never collide). Every write and delete is flushed with `PlayerPrefs.Save`, so a round recorded right before closing the game survives. Invalid or newer entries are logged and deleted.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PlayerPrefsPendingStatsStore.cs`

## Hereda / implementa
[[IPendingStatsStore]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerPrefsPendingStatsStore {
    +KeyFor(string ownerId)$ string
    +Load(string ownerId) PendingStats
    +Save(string ownerId, PendingStats pending) void
    +Clear(string ownerId) void
  }
  class IPendingStatsStore:::external
  <<interface>> IPendingStatsStore
  PlayerPrefsPendingStatsStore ..|> IPendingStatsStore
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IPendingStatsStore]], [[PendingStats]], [[PendingStatsCodec]], [[StatsReadResult]]

## Usado por
[[NetworkRuntimeAnchor]]
