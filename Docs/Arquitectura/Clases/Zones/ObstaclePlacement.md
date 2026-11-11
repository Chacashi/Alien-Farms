---
modulo: Zones
namespace: AlienFarms.Zones
tipo: struct
archivo: "Assets/Scripts/Zones/ObstaclePlacement.cs"
tags: [clase, modulo/zones]
---
# ObstaclePlacement

> Planner output for one slot: chosen table variant (-1 = empty) and yaw in degrees.

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/ObstaclePlacement.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstaclePlacement {
    <<struct>>
    +int Variant
    +float Yaw
    +bool IsActive
    +Clear() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ObstacleField]], [[ObstacleLayoutPlanner]]
