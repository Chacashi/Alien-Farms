---
modulo: Zones
namespace: AlienFarms.Zones
tipo: MonoBehaviour
archivo: "Assets/Scripts/Zones/ObstacleField.cs"
tags: [clase, modulo/zones]
---
# ObstacleField

> Scene composition root of the random obstacles of the Animal Zone. Holds the scene-placed `ObstacleSlot`s and, on `Regenerate`, asks `ObstacleLayoutPlanner` which slots get which variant and yaw, then toggles the pre-instantiated children (no Instantiate/Destroy, no positions moved). The first layout is rolled in Awake, before the animal spawner's Start populates the zone. As a `SimulationRole.Replica` it never rolls a layout: the authority's one is applied with `ApplyLayout` (captured there with `CaptureLayout`).

**Módulo:** [[Modulo Zones|Zones]] · **Namespace:** `AlienFarms.Zones` · **Archivo:** `Assets/Scripts/Zones/ObstacleField.cs`

## Hereda / implementa
`MonoBehaviour`, [[ISimulationRoleTarget]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleField {
    <<MonoBehaviour>>
    -ObstacleLayoutSettings _settings
    -ObstacleSlot[] _slots
    -LayerMask _clearanceMask
    -AnimalZone _animalZone
    +int ActiveCount
    +int SlotCount
    +SimulationRole Role
    +SetSimulationRole(SimulationRole role) void
    +CaptureLayout(ObstaclePlacement destination) int
    +ApplyLayout(ObstaclePlacement layout) bool
    +Regenerate() void
  }
  class AnimalZone:::external
  <<MonoBehaviour>> AnimalZone
  class IRandomSource:::external
  <<interface>> IRandomSource
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class ObstacleLayoutPlanner:::external
  class ObstacleLayoutSettings:::external
  <<ScriptableObject>> ObstacleLayoutSettings
  class ObstaclePlacement:::external
  <<struct>> ObstaclePlacement
  class ObstacleSlot:::external
  <<MonoBehaviour>> ObstacleSlot
  class ObstacleSlotInfo:::external
  <<struct>> ObstacleSlotInfo
  class ObstacleVariantRule:::external
  <<struct>> ObstacleVariantRule
  ObstacleField --> AnimalZone
  ObstacleField --> IRandomSource
  ObstacleField --> ObstacleLayoutPlanner
  ObstacleField --> ObstacleLayoutSettings
  ObstacleField --> ObstaclePlacement
  ObstacleField --> ObstacleSlot
  ObstacleField --> ObstacleSlotInfo
  ObstacleField --> ObstacleVariantRule
  ObstacleField ..|> ISimulationRoleTarget
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AnimalZone]], [[IRandomSource]], [[ISimulationRoleTarget]], [[LayerNames]], [[ObstacleLayoutPlanner]], [[ObstacleLayoutSettings]], [[ObstaclePlacement]], [[ObstacleSlot]], [[ObstacleSlotInfo]], [[ObstacleVariant]], [[ObstacleVariantData]], [[ObstacleVariantRule]], [[ObstacleWeightEntry]], [[SimulationRole]], [[SystemRandomSource]]

## Usado por
[[MatchManager]], [[NetworkMatchState]], [[OnlineMatchBootstrap]]
