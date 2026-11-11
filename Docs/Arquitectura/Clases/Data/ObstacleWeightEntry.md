---
modulo: Data
namespace: AlienFarms.Data
tipo: struct
archivo: "Assets/Scripts/Data/ObstacleWeightEntry.cs"
tags: [clase, modulo/data]
---
# ObstacleWeightEntry

> One weighted obstacle variant in `ObstacleLayoutSettings`.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/ObstacleWeightEntry.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleWeightEntry {
    <<struct>>
    -ObstacleVariantData _variant
    -int _weight
    +ObstacleVariantData Variant
    +int Weight
  }
  class ObstacleVariantData:::external
  <<ScriptableObject>> ObstacleVariantData
  ObstacleWeightEntry --> ObstacleVariantData
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ObstacleVariantData]]

## Usado por
[[ObstacleField]], [[ObstacleLayoutSettings]]
