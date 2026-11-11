---
modulo: Zones
namespace: AlienFarms.Zones
tipo: static
archivo: "Assets/Scripts/Zones/ZoneBounds.cs"
tags: [clase, modulo/zones]
---
# ZoneBounds

> Shared helpers to read zone areas from BoxColliders and test planar containment.

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/ZoneBounds.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ZoneBounds {
    <<static>>
    +Compute(BoxCollider box, Transform fallback)$ Bounds
    +ContainsPlanar(Bounds bounds, Vector3 point)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[AnimalZone]], [[PlayerZone]]
