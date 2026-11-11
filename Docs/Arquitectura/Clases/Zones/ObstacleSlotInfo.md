---
modulo: Zones
namespace: AlienFarms.Zones
tipo: struct
archivo: "Assets/Scripts/Zones/ObstacleSlotInfo.cs"
tags: [clase, modulo/zones]
---
# ObstacleSlotInfo

> Planner input for one obstacle slot. Plain value type, rebuilt in place every round.

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/ObstacleSlotInfo.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleSlotInfo {
    <<struct>>
    +Vector2 Position
    +int Mirror
    +uint VariantMask
    +bool Blocked
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ObstacleField]], [[ObstacleLayoutPlanner]]
