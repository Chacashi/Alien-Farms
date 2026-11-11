---
modulo: Animals
namespace: AlienFarms.Animals
tipo: interface
archivo: "Assets/Scripts/Animals/IAnimalNetworkHook.cs"
tags: [clase, modulo/animals]
---
# IAnimalNetworkHook

> Lets a network layer follow the authority's animal lifecycle. Without a hook, animals go straight back to their pool on despawn (local mode).

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/IAnimalNetworkHook.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class IAnimalNetworkHook {
    <<interface>>
    +OnSpawned(Animal animal) void
    +TryDespawn(Animal animal) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]]

## Usado por
[[Animal]], [[AnimalSpawner]], [[AnimalTypePool]]
