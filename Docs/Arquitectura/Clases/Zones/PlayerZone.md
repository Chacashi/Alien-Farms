---
modulo: Zones
namespace: AlienFarms.Zones
tipo: MonoBehaviour
archivo: "Assets/Scripts/Zones/PlayerZone.cs"
tags: [clase, modulo/zones]
---
# PlayerZone

> A player's pen. Its BoxCollider defines the capture area: an animal that lands inside counts for the owner while it stays captured. A child barrier (layer ZoneBarrier) keeps captured animals in; both players walk through it.

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/PlayerZone.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerZone {
    <<MonoBehaviour>>
    -PlayerId _owner
    -BoxCollider _area
    -Collider _barrier
    +PlayerId Owner
    +Bounds Bounds
    +Contains(Vector3 position) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LayerNames]], [[PlayerId]], [[ZoneBounds]]

## Usado por
[[PlayerZoneRegistry]]
