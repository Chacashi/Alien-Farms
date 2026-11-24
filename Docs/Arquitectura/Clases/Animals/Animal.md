---
modulo: Animals
namespace: AlienFarms.Animals
tipo: abstract
archivo: "Assets/Scripts/Animals/Animal.cs"
tags: [clase, modulo/animals]
---
# Animal

> Pooled, Rigidbody-driven animal that wanders inside the Animal Zone, flees from nearby players and can be carried and thrown by a player. Derived types only change data and the flee direction. State flow: Wandering <-> Fleeing -> Carried -> (any release) Airborne -> (lands) Captured when it settles inside a player zone (counts for that zone's owner), otherwise Wandering. Captured -> Carried when either player grabs it. Only the private transition methods write the state; the transitions into and out of Captured are the single source of `GameEvents.OnAnimalCaptured` / `GameEvents.OnAnimalLeftZone`. As a `SimulationRole.Replica` it runs no AI, landing or grabbing and raises no GameEvents: its body is kinematic and its state comes from `ApplyReplicatedState`.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/Animal.cs`

## Hereda / implementa
`MonoBehaviour`, [[IThrowable]], [[IScareable]], [[ISimulationRoleTarget]]

## Derivados / implementaciones
[[Chicken]], [[Cow]], [[Sheep]]

## Diagrama
```mermaid
classDiagram
  direction LR
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
  class AnimalData:::external
  <<ScriptableObject>> AnimalData
  class AnimalTypePool:::external
  class AvoidanceSide:::external
  <<struct>> AvoidanceSide
  class CornerEscape:::external
  <<struct>> CornerEscape
  class IAnimalNetworkHook:::external
  <<interface>> IAnimalNetworkHook
  class IScareable:::external
  <<interface>> IScareable
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class IThrowable:::external
  <<interface>> IThrowable
  class IZoneResolver:::external
  <<interface>> IZoneResolver
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
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AirborneOutcome]], [[AnimalData]], [[AnimalLanding]], [[AnimalState]], [[AnimalSteering]], [[AnimalTypePool]], [[AvoidanceSide]], [[Bomb]], [[CornerEscape]], [[DropReason]], [[GameEvents]], [[IAnimalNetworkHook]], [[IScareable]], [[ISimulationRoleTarget]], [[IThrowable]], [[IZoneResolver]], [[LayerNames]], [[ObstacleAvoidance]], [[PlayerId]], [[SimulationRole]], [[ThrowSettings]]

## Usado por
[[AnimalData]], [[AnimalSfxPlayer]], [[AnimalSpawner]], [[AnimalTypePool]], [[Bomb]], [[BombSpawner]], [[Chicken]], [[Cow]], [[GameEvents]], [[IAnimalNetworkHook]], [[LayerNames]], [[MatchManager]], [[NetworkAnimal]], [[NetworkAnimalPoolHandler]], [[NetworkSpawnRegistry]], [[PlayerGrabber]], [[Sheep]], [[ThirdPersonCamera]], [[ThrowTrajectoryView]]

## Eventos que publica
[[Eventos GameEvents#OnAnimalCaptured|OnAnimalCaptured]], [[Eventos GameEvents#OnAnimalDropped|OnAnimalDropped]], [[Eventos GameEvents#OnAnimalGrabbed|OnAnimalGrabbed]], [[Eventos GameEvents#OnAnimalLeftZone|OnAnimalLeftZone]]
