---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/ObstacleLayoutSettings.cs"
tags: [clase, modulo/data]
---
# ObstacleLayoutSettings

> Rules for the random obstacle layout of the Animal Zone (rolled every new round).

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/ObstacleLayoutSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleLayoutSettings {
    <<ScriptableObject>>
    -int _minActive
    -int _maxActive
    -ObstacleWeightEntry[] _variants
    -float _minSpacing
    -float _clearanceProbeHeight
    -float _minEdgeDistance
    -bool _useFixedSeed
    -int _seed
    +int MinActive
    +int MaxActive
    +IReadOnlyList~ObstacleWeightEntry~ Variants
    +float MinSpacing
    +bool MirrorAcrossCenter
    ...5 más
  }
  class ObstacleWeightEntry:::external
  <<struct>> ObstacleWeightEntry
  ObstacleLayoutSettings --> ObstacleWeightEntry
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ObstacleVariantData]], [[ObstacleWeightEntry]]

## Usado por
[[ObstacleField]]
