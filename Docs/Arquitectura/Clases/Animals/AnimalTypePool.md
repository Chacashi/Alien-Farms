---
modulo: Animals
namespace: AlienFarms.Animals
tipo: class
archivo: "Assets/Scripts/Animals/AnimalTypePool.cs"
tags: [clase, modulo/animals]
---
# AnimalTypePool

> Object pool for a single `AnimalData` type. Instances are created inactive under a container; activation is done by `Animal.Spawn` after positioning.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/AnimalTypePool.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
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
  class Animal:::external
  <<abstract>> Animal
  class AnimalData:::external
  <<ScriptableObject>> AnimalData
  class IAnimalNetworkHook:::external
  <<interface>> IAnimalNetworkHook
  class IZoneResolver:::external
  <<interface>> IZoneResolver
  AnimalTypePool --> Animal
  AnimalTypePool --> AnimalData
  AnimalTypePool --> IAnimalNetworkHook
  AnimalTypePool --> IZoneResolver
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[AnimalData]], [[IAnimalNetworkHook]], [[IZoneResolver]], [[SimulationRole]]

## Usado por
[[Animal]], [[AnimalSpawner]]
