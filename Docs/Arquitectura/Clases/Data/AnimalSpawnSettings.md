---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/AnimalSpawnSettings.cs"
tags: [clase, modulo/data]
---
# AnimalSpawnSettings

> Population and placement rules for the animal spawner.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/AnimalSpawnSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AnimalSpawnSettings {
    <<ScriptableObject>>
    -AnimalSpawnEntry[] _entries
    -int _initialCount
    -int _targetFreePopulation
    -float _respawnInterval
    -int _maxActiveAnimals
    -int _poolHardCap
    -int _prewarmPerType
    -float _spawnRadius
    -float _spawnClearance
    -int _maxPlacementAttempts
    -float _killHeight
    +AnimalSpawnEntry[] Entries
    +int InitialCount
    ...9 más
  }
  class AnimalSpawnEntry:::external
  <<struct>> AnimalSpawnEntry
  AnimalSpawnSettings --> AnimalSpawnEntry
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AnimalSpawnEntry]]

## Usado por
[[AnimalSpawner]]
