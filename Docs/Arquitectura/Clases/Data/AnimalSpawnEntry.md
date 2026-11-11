---
modulo: Data
namespace: AlienFarms.Data
tipo: struct
archivo: "Assets/Scripts/Data/AnimalSpawnEntry.cs"
tags: [clase, modulo/data]
---
# AnimalSpawnEntry

> One weighted animal type in `AnimalSpawnSettings`.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/AnimalSpawnEntry.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AnimalSpawnEntry {
    <<struct>>
    -AnimalData _data
    -int _weight
    +AnimalData Data
    +int Weight
  }
  class AnimalData:::external
  <<ScriptableObject>> AnimalData
  AnimalSpawnEntry --> AnimalData
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AnimalData]]

## Usado por
[[AnimalSpawnSettings]], [[AnimalSpawner]]
