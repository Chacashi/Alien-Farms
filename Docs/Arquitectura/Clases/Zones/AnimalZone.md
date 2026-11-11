---
modulo: Zones
namespace: AlienFarms.Zones
tipo: MonoBehaviour
archivo: "Assets/Scripts/Zones/AnimalZone.cs"
tags: [clase, modulo/zones]
---
# AnimalZone

> Central area where animals live. Exposes the world-space bounds of its BoxCollider so animals can be confined to it. Animals receive plain `UnityEngine.Bounds` and never depend on this type.

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/AnimalZone.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AnimalZone {
    <<MonoBehaviour>>
    -BoxCollider _box
    +Bounds Bounds
    +Contains(Vector3 position) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ZoneBounds]]

## Usado por
[[AnimalSpawner]], [[BombSpawner]], [[ObstacleField]]
