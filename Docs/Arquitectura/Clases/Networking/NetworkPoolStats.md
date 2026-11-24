---
modulo: Networking
namespace: AlienFarms.Networking.Pooling
tipo: struct
archivo: "Assets/Scripts/Networking/Pooling/NetworkPoolStats.cs"
tags: [clase, modulo/networking]
---
# NetworkPoolStats

> Pool counters for the debug overlay: instances in play, waiting in the pool, and ever created.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Pooling` · **Archivo:** `Assets/Scripts/Networking/Pooling/NetworkPoolStats.cs`

## Hereda / implementa
`IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkPoolStats {
    <<struct>>
    +int Active
    +int Inactive
    +int Created
    +Equals(NetworkPoolStats other) bool
    +Equals(object obj) bool
    +GetHashCode() int
    +bool operator$
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[NetworkPoolLog]], [[NetworkSpawnRegistry]], [[NetworkStatsFormatter]], [[NetworkStatsSample]], [[PoolStatsView]]
