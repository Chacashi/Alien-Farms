---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/ObstacleVariantData.cs"
tags: [clase, modulo/data]
---
# ObstacleVariantData

> One obstacle type (fence, UFO disc...) and the yaws it may be placed with.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/ObstacleVariantData.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleVariantData {
    <<ScriptableObject>>
    +IReadOnlyList~float~ AllowedYaws
    +int RotationalSymmetry
    +bool IsMirrorSymmetric
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ObstacleField]], [[ObstacleLayoutSettings]], [[ObstacleVariant]], [[ObstacleWeightEntry]]
