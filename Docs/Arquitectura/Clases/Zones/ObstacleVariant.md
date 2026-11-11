---
modulo: Zones
namespace: AlienFarms.Zones
tipo: MonoBehaviour
archivo: "Assets/Scripts/Zones/ObstacleVariant.cs"
tags: [clase, modulo/zones]
---
# ObstacleVariant

> Root of one obstacle variant prefab (pivot at ground level). Pre-instantiated, inactive, under an `ObstacleSlot`; the slot only toggles it and sets its yaw. Its geometry lives on the Environment layer.

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/ObstacleVariant.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleVariant {
    <<MonoBehaviour>>
    -ObstacleVariantData _data
    +ObstacleVariantData Data
  }
  class ObstacleVariantData:::external
  <<ScriptableObject>> ObstacleVariantData
  ObstacleVariant --> ObstacleVariantData
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LayerNames]], [[ObstacleVariantData]]

## Usado por
[[ObstacleField]], [[ObstacleSlot]]
