---
modulo: Networking
namespace: AlienFarms.Networking.Prediction
tipo: class
archivo: "Assets/Scripts/Networking/Prediction/SnapshotBuffer.cs"
tags: [clase, modulo/networking]
---
# SnapshotBuffer

> Fixed ring of received `PlayerSnapshot`s, oldest first and strictly increasing by tick. Out-of-order or duplicate snapshots are dropped; when full the oldest is overwritten. A snapshot from another `PlayerSnapshot.TeleportEpoch` clears the history, so all buffered snapshots share one epoch and nothing ever interpolates across a teleport.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Prediction` · **Archivo:** `Assets/Scripts/Networking/Prediction/SnapshotBuffer.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SnapshotBuffer {
    +int Count
    +int Capacity
    +TryGetNewest(PlayerSnapshot snapshot) bool
    +Add(PlayerSnapshot snapshot) SnapshotAddResult
    +Clear() void
  }
  class PlayerSnapshot:::external
  <<struct>> PlayerSnapshot
  SnapshotBuffer --> PlayerSnapshot
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerSnapshot]], [[SnapshotAddResult]]

## Usado por
[[NetworkPlayerMotion]], [[SnapshotInterpolator]]
