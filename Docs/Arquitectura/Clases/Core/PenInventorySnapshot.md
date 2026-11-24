---
modulo: Core
namespace: AlienFarms.Core
tipo: static
archivo: "Assets/Scripts/Core/PenInventorySnapshot.cs"
tags: [clase, modulo/core]
---
# PenInventorySnapshot

> Last pen inventory count raised per player and animal type, so a view enabled late (or re-enabled) can catch up without waiting for the next `GameEvents.OnPenInventoryChanged`. Written only by `GameEvents.RaisePenInventoryChanged` and cleared with the rest of the game event statics.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/PenInventorySnapshot.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PenInventorySnapshot {
    <<static>>
    +Get(PlayerId playerId, int typeIndex)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]]

## Usado por
[[GameEvents]], [[OnlineMatchStatsRecorder]], [[PenInventoryView]]
