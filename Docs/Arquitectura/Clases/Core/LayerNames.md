---
modulo: Core
namespace: AlienFarms.Core
tipo: static
archivo: "Assets/Scripts/Core/LayerNames.cs"
tags: [clase, modulo/core]
---
# LayerNames

> Physics layer names as configured in Tags and Layers. Resolve to indices once, never per frame.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/LayerNames.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LayerNames {
    <<static>>
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[Bomb]]

## Usado por
[[Animal]], [[AnimalSpawner]], [[Bomb]], [[BombSpawner]], [[ObstacleField]], [[ObstacleVariant]], [[PlayerAim]], [[PlayerGrabber]], [[PlayerTackle]], [[PlayerZone]], [[ThirdPersonCamera]], [[ThrowTrajectoryView]]
