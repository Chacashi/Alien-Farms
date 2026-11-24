---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: enumeration
archivo: "Assets/Scripts/Networking/Persistence/StatsReadResult.cs"
tags: [clase, modulo/networking]
---
# StatsReadResult

> Outcome of decoding persisted stats (cloud record or local pending).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/StatsReadResult.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class StatsReadResult {
    <<enumeration>>
    Ok
    Missing
    NewerSchema
    Corrupt
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Ok`, `Missing`, `NewerSchema`, `Corrupt`

## Usado por
[[CloudSavePlayerStatsStore]], [[PendingStatsCodec]], [[PlayerPrefsPendingStatsStore]], [[PlayerStatsCodec]], [[PlayerStatsLedger]], [[PlayerStatsSync]], [[StatsStoreResult]]
