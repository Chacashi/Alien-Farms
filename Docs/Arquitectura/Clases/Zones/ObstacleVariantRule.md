---
modulo: Zones
namespace: AlienFarms.Zones
tipo: struct
archivo: "Assets/Scripts/Zones/ObstacleVariantRule.cs"
tags: [clase, modulo/zones]
---
# ObstacleVariantRule

> Planner rule for one entry of the variant table: relative weight and allowed yaws (degrees).

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/ObstacleVariantRule.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleVariantRule {
    <<struct>>
    +int Weight
    +float[] Yaws
    +bool IsMirrorSymmetric
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ObstacleField]], [[ObstacleLayoutPlanner]]
