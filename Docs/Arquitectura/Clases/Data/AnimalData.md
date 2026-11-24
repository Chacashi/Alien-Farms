---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/AnimalData.cs"
tags: [clase, modulo/data]
---
# AnimalData

> Tuning and prefab for one animal type.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/AnimalData.cs`

## Hereda / implementa
`ScriptableObject`

## Derivados / implementaciones
[[ChickenData]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class AnimalData {
    <<ScriptableObject>>
    -Animal _prefab
    -Sprite _icon
    -int _points
    -float _mass
    -float _wanderSpeed
    -float _fleeSpeed
    -float _acceleration
    -float _turnSpeed
    -float _detectionRadius
    -float _fleeExitRadius
    -float _minFleeTime
    -float _detectionInterval
    -float _wanderRadius
    ...48 más
  }
  class Animal:::external
  <<abstract>> Animal
  class ThrowSettings:::external
  <<ScriptableObject>> ThrowSettings
  AnimalData --> Animal
  AnimalData --> ThrowSettings
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[SoundCue]], [[ThrowSettings]]

## Usado por
[[Animal]], [[AnimalSfxPlayer]], [[AnimalSpawnEntry]], [[AnimalSpawner]], [[AnimalTypePool]], [[ChickenData]], [[MatchManager]], [[MatchScore]], [[NetworkAnimalPoolHandler]], [[NetworkSpawnRegistry]]
