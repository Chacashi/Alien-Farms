---
modulo: Animals
namespace: AlienFarms.Animals
tipo: MonoBehaviour
archivo: "Assets/Scripts/Animals/AnimalSpawner.cs"
tags: [clase, modulo/animals]
---
# AnimalSpawner

> Scene composition root for animals: owns one pool per type, spawns the initial population around a scene-placed center and refills it over time while the free population is below target. As a `SimulationRole.Replica` it only keeps the pools (for replicated animals) and never populates.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/AnimalSpawner.cs`

## Hereda / implementa
`MonoBehaviour`, [[ISimulationRoleTarget]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class AnimalSpawner {
    <<MonoBehaviour>>
    -AnimalSpawnSettings _settings
    -Transform _spawnCenter
    -AnimalZone _animalZone
    -PlayerZoneRegistry _playerZones
    -Transform _animalsContainer
    +int ActiveCount
    +SimulationRole Role
    +int TypeCount
    +SetSimulationRole(SimulationRole role) void
    +SetNetworkHook(IAnimalNetworkHook hook) void
    +TryGetTypeIndex(AnimalData data, int typeIndex) bool
    +TryGetPool(AnimalData data, AnimalTypePool pool) bool
    +Despawn(Animal animal) void
    +ResetPopulation() void
  }
  class Animal:::external
  <<abstract>> Animal
  class AnimalSpawnSettings:::external
  <<ScriptableObject>> AnimalSpawnSettings
  class AnimalTypePool:::external
  class AnimalZone:::external
  <<MonoBehaviour>> AnimalZone
  class IAnimalNetworkHook:::external
  <<interface>> IAnimalNetworkHook
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class PlayerZoneRegistry:::external
  <<MonoBehaviour>> PlayerZoneRegistry
  AnimalSpawner --> Animal
  AnimalSpawner --> AnimalSpawnSettings
  AnimalSpawner --> AnimalTypePool
  AnimalSpawner --> AnimalZone
  AnimalSpawner --> IAnimalNetworkHook
  AnimalSpawner --> PlayerZoneRegistry
  AnimalSpawner ..|> ISimulationRoleTarget
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[AnimalData]], [[AnimalSpawnEntry]], [[AnimalSpawnSettings]], [[AnimalState]], [[AnimalSteering]], [[AnimalTypePool]], [[AnimalZone]], [[Bomb]], [[IAnimalNetworkHook]], [[ISimulationRoleTarget]], [[IZoneResolver]], [[LayerNames]], [[PlayerZoneRegistry]], [[SimulationRole]]

## Usado por
[[MatchManager]], [[NetworkAnimalPoolHandler]], [[NetworkSpawnRegistry]], [[OnlineMatchBootstrap]]
