---
tags: [modulo, modulo/animals]
---
# Módulo Animals

Carpeta `Assets/Scripts/Animals/` · 14 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class AirborneOutcome {
    <<enumeration>>
    Keep
    Land
    Despawn
    SlideOff
    Relocate
  }
  class Animal {
    <<abstract>>
    -Rigidbody _rb
    -Collider _collider
    -Transform _visual
    +event Action~Animal~ StateChanged
    +AnimalData Data
    +AnimalState State
    +SimulationRole Role
    +bool IsFree
    +PlayerId Owner
    +bool CanBeGrabbed
    +bool CanBeThrown
    +ThrowSettings ThrowSettings
    +float CollisionRadius
    ...18 más
  }
  class AnimalLanding {
    <<static>>
    +IsStuckOnEnvironment(bool touchingEnvironment, bool grounded, bool onPlayer, float speedSqr, float maxStuckSpeed)$ bool
    +ShouldPushOff(bool stuck, float now, float stuckSince, float maxStuckTime, int pushesUsed, int maxPushes)$ bool
    +PushOffDirection(Vector3 bodyCenter, Vector3 contactPoint, Vector3 contactNormal, Vector3 fallback)$ Vector3
    +TrackPlayerContact()$ void
    +Evaluate()$ AirborneOutcome
  }
  class AnimalSpawner {
    <<MonoBehaviour>>
    -AnimalSpawnSettings _settings
    -Transform _spawnCenter
    -AnimalZone _animalZone
    -PlayerZoneRegistry _playerZones
    -Transform _animalsContainer
    +int ActiveCount
    +SimulationRole Role
    +SetSimulationRole(SimulationRole role) void
    +SetNetworkHook(IAnimalNetworkHook hook) void
    +TryGetTypeIndex(AnimalData data, int typeIndex) bool
    +TryGetPool(AnimalData data, AnimalTypePool pool) bool
    +Despawn(Animal animal) void
    +ResetPopulation() void
  }
  class AnimalState {
    <<enumeration>>
    Inactive
    Wandering
    Fleeing
    Carried
    Captured
    Airborne
  }
  class AnimalSteering {
    <<static>>
    +ConfineDirection(Vector3 position, Vector3 direction, Bounds bounds, float margin, float lookahead)$ Vector3
    +EscapeAlongEdge(Vector3 position, Vector3 awayFromThreat, Bounds bounds, float margin, float lookahead)$ Vector3
    +SteerFlee()$ Vector3
    +RandomPointInside(Vector3 around, float radius, Bounds bounds, float margin)$ Vector3
    +ClampInside(Vector3 point, Bounds bounds, float margin)$ Vector3
  }
  class AnimalTypePool {
    +AnimalData Data
    +int CountActive
    +int CountInactive
    +IAnimalNetworkHook NetworkHook
    +Get() Animal
    +Release(Animal animal) void
    +SetNetworkHook(IAnimalNetworkHook hook) void
    +GetForReplica(Vector3 position, Quaternion rotation) Animal
    +Prewarm(int count) void
  }
  class AvoidanceSide {
    <<struct>>
    +int Side
    +float Until
    +int ObstacleId
    +IsActive(float now) bool
    +Holds(float now, int obstacleId) bool
    +Begin(int side, float until, int obstacleId) void
    +Clear() void
  }
  class Chicken {
    #OnInitialized() void
    #ComputeFleeDirection(Vector3 awayFromThreat) Vector3
  }
  class CornerEscape {
    <<struct>>
    +Vector3 Direction
    +float Until
    +IsActive(float now) bool
    +Begin(Vector3 direction, float until) void
    +Clear() void
  }
  class Cow {
    #ComputeFleeDirection(Vector3 awayFromThreat) Vector3
  }
  class IAnimalNetworkHook {
    <<interface>>
    +OnSpawned(Animal animal) void
    +TryDespawn(Animal animal) bool
  }
  class ObstacleAvoidance {
    <<static>>
    +Avoid()$ Vector3
    +Detour(Vector3 desired, Vector3 hitNormal, float hitDistance, float probeDistance, int side)$ Vector3
    +SideOf(Vector3 direction, Vector3 hitNormal)$ int
  }
  class Sheep {
    #ComputeFleeDirection(Vector3 awayFromThreat) Vector3
  }
  class AnimalData:::external
  <<ScriptableObject>> AnimalData
  class AnimalSpawnSettings:::external
  <<ScriptableObject>> AnimalSpawnSettings
  class AnimalZone:::external
  <<MonoBehaviour>> AnimalZone
  class ChickenData:::external
  class IScareable:::external
  <<interface>> IScareable
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class IThrowable:::external
  <<interface>> IThrowable
  class IZoneResolver:::external
  <<interface>> IZoneResolver
  class PlayerZoneRegistry:::external
  <<MonoBehaviour>> PlayerZoneRegistry
  class ThrowSettings:::external
  <<ScriptableObject>> ThrowSettings
  Animal --> AnimalData
  Animal --> AnimalTypePool
  Animal --> AvoidanceSide
  Animal --> CornerEscape
  Animal --> IAnimalNetworkHook
  Animal --> IZoneResolver
  Animal --> ThrowSettings
  Animal ..|> IScareable
  Animal ..|> ISimulationRoleTarget
  Animal ..|> IThrowable
  AnimalSpawner --> Animal
  AnimalSpawner --> AnimalSpawnSettings
  AnimalSpawner --> AnimalTypePool
  AnimalSpawner --> AnimalZone
  AnimalSpawner --> IAnimalNetworkHook
  AnimalSpawner --> PlayerZoneRegistry
  AnimalSpawner ..|> ISimulationRoleTarget
  AnimalTypePool --> Animal
  AnimalTypePool --> AnimalData
  AnimalTypePool --> IAnimalNetworkHook
  AnimalTypePool --> IZoneResolver
  Chicken --> ChickenData
  Chicken --|> Animal
  Cow --|> Animal
  Sheep --|> Animal
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[AirborneOutcome]] | enumeration | What an airborne animal does this physics step (see `AnimalLanding.Evaluate`). |
| [[Animal]] | abstract | Pooled, Rigidbody-driven animal that wanders inside the Animal Zone, flees from nearby players and can be carried and thrown by a player. De… |
| [[AnimalLanding]] | static | Pure rule that decides what an airborne animal does this physics step. |
| [[AnimalSpawner]] | MonoBehaviour | Scene composition root for animals: owns one pool per type, spawns the initial population around a scene-placed center and refills it over t… |
| [[AnimalState]] | enumeration | High-level behaviour state of an `Animal`. |
| [[AnimalSteering]] | static | Pure planar (X/Z) steering helpers that keep animals inside a `Bounds` shrunk by a margin (the "inner rectangle"). Stateless, allocation-fre… |
| [[AnimalTypePool]] | class | Object pool for a single `AnimalData` type. Instances are created inactive under a container; activation is done by `Animal.Spawn` after pos… |
| [[AvoidanceSide]] | struct | Hysteresis state for `ObstacleAvoidance.Avoid`: once an animal picks a side to go around an obstacle, it keeps that side for that same obsta… |
| [[Chicken]] | class | Fast, light animal that zigzags while fleeing. |
| [[CornerEscape]] | struct | Hysteresis state for `AnimalSteering.SteerFlee`: once a cornered animal picks an edge to run along, it keeps that axis direction at least un… |
| [[Cow]] | class | Heaviest animal: slow to react and to flee (straight line), worth the most points. Its carry and throw penalties live in its `AlienFarms.Dat… |
| [[IAnimalNetworkHook]] | interface | Lets a network layer follow the authority's animal lifecycle. Without a hook, animals go straight back to their pool on despawn (local mode)… |
| [[ObstacleAvoidance]] | static | Pure planar obstacle avoidance: given the desired direction and a probe hit in front of the animal, slides along the obstacle tangent and ad… |
| [[Sheep]] | class | Heavy, slow animal that flees in a straight line. |

## Depende de
[[Modulo Core|Core]] (15), [[Modulo Data|Data]] (7), [[Modulo Items|Items]] (2), [[Modulo Zones|Zones]] (2)