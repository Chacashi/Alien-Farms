---
modulo: Zones
namespace: AlienFarms.Zones
tipo: MonoBehaviour
archivo: "Assets/Scripts/Zones/ObstacleSlot.cs"
tags: [clase, modulo/zones]
---
# ObstacleSlot

> A scene-placed spot that may hold one obstacle per round. All its variants are pre-instantiated children, inactive by default; `Apply` enables at most one and sets its yaw. Positions are never changed by code.

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/ObstacleSlot.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleSlot {
    <<MonoBehaviour>>
    -ObstacleSlot _mirror
    -float _clearanceRadius
    -ObstacleVariant[] _variants
    +Vector2 PlanarPosition
    +float ClearanceRadius
    +ObstacleSlot Mirror
    +int VariantCount
    +int ActiveVariant
    +GetVariant(int index) ObstacleVariant
    +Apply(int localVariantIndex, float yaw) void
  }
  class ObstacleVariant:::external
  <<MonoBehaviour>> ObstacleVariant
  ObstacleSlot --> ObstacleVariant
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ObstacleVariant]]

## Usado por
[[ObstacleField]]
