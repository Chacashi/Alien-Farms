---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: interface
archivo: "Assets/Scripts/Networking/Persistence/IPendingStatsStore.cs"
tags: [clase, modulo/networking]
---
# IPendingStatsStore

> Local storage of the unconfirmed stats, one entry per account (survives closing the game). Implementations ignore (and delete) invalid or newer entries.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/IPendingStatsStore.cs`

## Derivados / implementaciones
[[PlayerPrefsPendingStatsStore]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IPendingStatsStore {
    <<interface>>
    +Load(string ownerId) PendingStats
    +Save(string ownerId, PendingStats pending) void
    +Clear(string ownerId) void
  }
  class PendingStats:::external
  <<struct>> PendingStats
  IPendingStatsStore --> PendingStats
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PendingStats]]

## Usado por
[[PlayerPrefsPendingStatsStore]], [[PlayerStatsLedger]]
