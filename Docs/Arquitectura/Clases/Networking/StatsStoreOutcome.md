---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: enumeration
archivo: "Assets/Scripts/Networking/Persistence/StatsStoreOutcome.cs"
tags: [clase, modulo/networking]
---
# StatsStoreOutcome

> How an `IPlayerStatsStore` call ended.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/StatsStoreOutcome.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class StatsStoreOutcome {
    <<enumeration>>
    Ok
    Conflict
    RetryableError
    Error
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Ok`, `Conflict`, `RetryableError`, `Error`

## Usado por
[[PlayerStatsSync]], [[StatsStoreResult]]
