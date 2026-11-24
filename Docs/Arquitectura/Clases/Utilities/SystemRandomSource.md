---
modulo: Utilities
namespace: AlienFarms.Utilities
tipo: class
archivo: "Assets/Scripts/Utilities/SystemRandomSource.cs"
tags: [clase, modulo/utilities]
---
# SystemRandomSource

> `IRandomSource` backed by its own `System.Random`: the same seed always yields the same sequence, independently of anything else using UnityEngine.Random.

**Módulo:** [[Modulo Utilities|Utilities]] · **Namespace:** `AlienFarms.Utilities` · **Archivo:** `Assets/Scripts/Utilities/SystemRandomSource.cs`

## Hereda / implementa
[[IRandomSource]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class SystemRandomSource {
    +Range(int minInclusive, int maxExclusive) int
    +Value01() float
  }
  class IRandomSource:::external
  <<interface>> IRandomSource
  SystemRandomSource ..|> IRandomSource
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IRandomSource]]

## Usado por
[[AnimalSfxPlayer]], [[BombSfxPlayer]], [[BombSpawner]], [[CharacterSelectSfxPlayer]], [[MatchStatusSfxPlayer]], [[ObstacleField]], [[OnlineSessionSfxPlayer]]
