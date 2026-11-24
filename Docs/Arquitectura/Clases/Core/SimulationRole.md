---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/SimulationRole.cs"
tags: [clase, modulo/core]
---
# SimulationRole

> Who runs a simulated component. Authority runs the gameplay logic (local mode and the online server); Replica only shows state applied from outside (online clients). Every target defaults to Authority.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/SimulationRole.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SimulationRole {
    <<enumeration>>
    Authority
    Replica
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Authority`, `Replica`

## Usado por
[[Animal]], [[AnimalSpawner]], [[AnimalTypePool]], [[Bomb]], [[BombSpawner]], [[ISimulationRoleTarget]], [[NetworkAnimal]], [[NetworkAnimalPoolHandler]], [[NetworkBomb]], [[NetworkBombPoolHandler]], [[NetworkPlayer]], [[ObstacleField]], [[OnlineMatchBootstrap]], [[PlayerGrabber]], [[PlayerMovement]], [[PlayerStunReceiver]]
