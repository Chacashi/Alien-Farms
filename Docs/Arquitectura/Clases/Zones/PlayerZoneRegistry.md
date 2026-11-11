---
modulo: Zones
namespace: AlienFarms.Zones
tipo: MonoBehaviour
archivo: "Assets/Scripts/Zones/PlayerZoneRegistry.cs"
tags: [clase, modulo/zones]
---
# PlayerZoneRegistry

> Scene registry of the player pens (lives on the 'Zones' root). Animals ask it, through `IZoneResolver`, which pen they landed in.

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/PlayerZoneRegistry.cs`

## Hereda / implementa
`MonoBehaviour`, [[IZoneResolver]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerZoneRegistry {
    <<MonoBehaviour>>
    -PlayerZone[] _zones
    +TryGetZoneAt(Vector3 position, PlayerId owner, Bounds area) bool
  }
  class IZoneResolver:::external
  <<interface>> IZoneResolver
  class PlayerZone:::external
  <<MonoBehaviour>> PlayerZone
  PlayerZoneRegistry --> PlayerZone
  PlayerZoneRegistry ..|> IZoneResolver
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IZoneResolver]], [[PlayerId]], [[PlayerZone]]

## Usado por
[[AnimalSpawner]]
