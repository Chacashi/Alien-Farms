---
modulo: Core
namespace: AlienFarms.Core
tipo: interface
archivo: "Assets/Scripts/Core/ISimulationRoleTarget.cs"
tags: [clase, modulo/core]
---
# ISimulationRoleTarget

> A component whose simulation can be switched between Authority (default) and Replica.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/ISimulationRoleTarget.cs`

## Derivados / implementaciones
[[Animal]], [[AnimalSpawner]], [[Bomb]], [[BombSpawner]], [[ObstacleField]], [[PlayerGrabber]], [[PlayerMovement]], [[PlayerStunReceiver]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class ISimulationRoleTarget {
    <<interface>>
    +SimulationRole Role
    +SetSimulationRole(SimulationRole role) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[SimulationRole]]

## Usado por
[[Animal]], [[AnimalSpawner]], [[Bomb]], [[BombSpawner]], [[ObstacleField]], [[PlayerGrabber]], [[PlayerMovement]], [[PlayerStunReceiver]]
