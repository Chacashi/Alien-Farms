---
tags: [modulo, modulo/zones]
---
# Módulo Zones

Carpeta `Assets/Scripts/Zones/` · 11 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class AnimalZone {
    <<MonoBehaviour>>
    -BoxCollider _box
    +Bounds Bounds
    +Contains(Vector3 position) bool
  }
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
  class ObstacleLayoutPlanner {
    +int Capacity
    +Plan() int
  }
  class ObstaclePlacement {
    <<struct>>
    +int Variant
    +float Yaw
    +bool IsActive
    +Clear() void
  }
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
  class ObstacleSlotInfo {
    <<struct>>
    +Vector2 Position
    +int Mirror
    +uint VariantMask
    +bool Blocked
  }
  class ObstacleVariant {
    <<MonoBehaviour>>
    -ObstacleVariantData _data
    +ObstacleVariantData Data
  }
  class ObstacleVariantRule {
    <<struct>>
    +int Weight
    +float[] Yaws
    +bool IsMirrorSymmetric
  }
  class PlayerZone {
    <<MonoBehaviour>>
    -PlayerId _owner
    -BoxCollider _area
    -Collider _barrier
    +PlayerId Owner
    +Bounds Bounds
    +Contains(Vector3 position) bool
  }
  class PlayerZoneRegistry {
    <<MonoBehaviour>>
    -PlayerZone[] _zones
    +TryGetZoneAt(Vector3 position, PlayerId owner, Bounds area) bool
  }
  class ZoneBounds {
    <<static>>
    +Compute(BoxCollider box, Transform fallback)$ Bounds
    +ContainsPlanar(Bounds bounds, Vector3 point)$ bool
  }
  class IRandomSource:::external
  <<interface>> IRandomSource
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class IZoneResolver:::external
  <<interface>> IZoneResolver
  class ObstacleLayoutSettings:::external
  <<ScriptableObject>> ObstacleLayoutSettings
  class ObstacleVariantData:::external
  <<ScriptableObject>> ObstacleVariantData
  ObstacleField --> AnimalZone
  ObstacleField --> IRandomSource
  ObstacleField --> ObstacleLayoutPlanner
  ObstacleField --> ObstacleLayoutSettings
  ObstacleField --> ObstaclePlacement
  ObstacleField --> ObstacleSlot
  ObstacleField --> ObstacleSlotInfo
  ObstacleField --> ObstacleVariantRule
  ObstacleField ..|> ISimulationRoleTarget
  ObstacleSlot --> ObstacleVariant
  ObstacleVariant --> ObstacleVariantData
  PlayerZoneRegistry --> PlayerZone
  PlayerZoneRegistry ..|> IZoneResolver
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[AnimalZone]] | MonoBehaviour | Central area where animals live. Exposes the world-space bounds of its BoxCollider so animals can be confined to it. Animals receive plain `… |
| [[ObstacleField]] | MonoBehaviour | Scene composition root of the random obstacles of the Animal Zone. Holds the scene-placed `ObstacleSlot`s and, on `Regenerate`, asks `Obstac… |
| [[ObstacleLayoutPlanner]] | class | Pure obstacle layout picker (no Unity objects, no allocations after construction, EditMode-testable). Decides which slots get an obstacle, w… |
| [[ObstaclePlacement]] | struct | Planner output for one slot: chosen table variant (-1 = empty) and yaw in degrees. |
| [[ObstacleSlot]] | MonoBehaviour | A scene-placed spot that may hold one obstacle per round. All its variants are pre-instantiated children, inactive by default; `Apply` enabl… |
| [[ObstacleSlotInfo]] | struct | Planner input for one obstacle slot. Plain value type, rebuilt in place every round. |
| [[ObstacleVariant]] | MonoBehaviour | Root of one obstacle variant prefab (pivot at ground level). Pre-instantiated, inactive, under an `ObstacleSlot`; the slot only toggles it a… |
| [[ObstacleVariantRule]] | struct | Planner rule for one entry of the variant table: relative weight and allowed yaws (degrees). |
| [[PlayerZone]] | MonoBehaviour | A player's pen. Its BoxCollider defines the capture area: an animal that lands inside counts for the owner while it stays captured. A child … |
| [[PlayerZoneRegistry]] | MonoBehaviour | Scene registry of the player pens (lives on the 'Zones' root). Animals ask it, through `IZoneResolver`, which pen they landed in. |
| [[ZoneBounds]] | static | Shared helpers to read zone areas from BoxColliders and test planar containment. |

## Depende de
[[Modulo Core|Core]] (8), [[Modulo Data|Data]] (4), [[Modulo Utilities|Utilities]] (3)