---
modulo: Utilities
namespace: AlienFarms.Utilities
tipo: interface
archivo: "Assets/Scripts/Utilities/IRandomSource.cs"
tags: [clase, modulo/utilities]
---
# IRandomSource

> Injectable random number source, so pure logic can be seeded and tested deterministically (never touches the global UnityEngine.Random state).

**Módulo:** [[Modulo Utilities|Utilities]] · **Namespace:** `AlienFarms.Utilities` · **Archivo:** `Assets/Scripts/Utilities/IRandomSource.cs`

## Derivados / implementaciones
[[SystemRandomSource]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IRandomSource {
    <<interface>>
    +Range(int minInclusive, int maxExclusive) int
    +Value01() float
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[AnimalSfxPlayer]], [[AudioVariation]], [[BombPlacement]], [[BombSfxPlayer]], [[BombSpawnSchedule]], [[BombSpawner]], [[CharacterSelectSfxPlayer]], [[MatchStatusSfxPlayer]], [[ObstacleField]], [[ObstacleLayoutPlanner]], [[OnlineSessionSfxPlayer]], [[SfxVoicePool]], [[SystemRandomSource]]
