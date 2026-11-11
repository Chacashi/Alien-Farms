---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/ThrowSettings.cs"
tags: [clase, modulo/data]
---
# ThrowSettings

> Launch and trajectory-preview tuning for thrown objects.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/ThrowSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ThrowSettings {
    <<ScriptableObject>>
    -float _launchSpeed
    -float _baseElevation
    -float _minElevation
    -float _maxElevation
    -float _pitchInfluence
    -float _referenceCameraPitch
    -float _inheritVelocityFactor
    -float _previewMaxTime
    -int _previewStepsPerPoint
    +float LaunchSpeed
    +float BaseElevation
    +float MinElevation
    +float MaxElevation
    ...5 más
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[Animal]], [[AnimalData]], [[Bomb]], [[BombData]], [[IThrowable]], [[PlayerAim]], [[ThrowTrajectoryView]]
