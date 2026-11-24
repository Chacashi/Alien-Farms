---
modulo: Items
namespace: AlienFarms.Items
tipo: MonoBehaviour
archivo: "Assets/Scripts/Items/BombSpawner.cs"
tags: [clase, modulo/items]
---
# BombSpawner

> Scene composition root for stun bombs: owns the bomb pool and drops bombs at random free spots of the Animal Zone while a round is playing (timer paused while the max is in play). Bombs never exist outside Playing. As a `SimulationRole.Replica` it only keeps the pool (for replicated bombs) and never spawns or clears.

**Módulo:** [[Modulo Items|Items]] · **Namespace:** `AlienFarms.Items` · **Archivo:** `Assets/Scripts/Items/BombSpawner.cs`

## Hereda / implementa
`MonoBehaviour`, [[ISimulationRoleTarget]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombSpawner {
    <<MonoBehaviour>>
    -BombSpawnSettings _settings
    -BombData _data
    -AnimalZone _animalZone
    -Transform _bombsContainer
    +int ActiveCount
    +SimulationRole Role
    +int PoolCountActive
    +int PoolCountInactive
    +int PoolCountAll
    +int PrewarmCount
    +ResetBombs() void
    +SetSimulationRole(SimulationRole role) void
    +SetNetworkHook(IBombNetworkHook hook) void
    ...2 más
  }
  class AnimalZone:::external
  <<MonoBehaviour>> AnimalZone
  class Bomb:::external
  <<MonoBehaviour>> Bomb
  class BombData:::external
  <<ScriptableObject>> BombData
  class BombSpawnSchedule:::external
  class BombSpawnSettings:::external
  <<ScriptableObject>> BombSpawnSettings
  class IBombNetworkHook:::external
  <<interface>> IBombNetworkHook
  class IRandomSource:::external
  <<interface>> IRandomSource
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  BombSpawner --> AnimalZone
  BombSpawner --> Bomb
  BombSpawner --> BombData
  BombSpawner --> BombSpawnSchedule
  BombSpawner --> BombSpawnSettings
  BombSpawner --> IBombNetworkHook
  BombSpawner --> IRandomSource
  BombSpawner ..|> ISimulationRoleTarget
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[AnimalZone]], [[Bomb]], [[BombData]], [[BombPlacement]], [[BombSpawnSchedule]], [[BombSpawnSettings]], [[BombState]], [[GameEvents]], [[IBombNetworkHook]], [[IRandomSource]], [[ISimulationRoleTarget]], [[LayerNames]], [[MatchPhase]], [[SimulationRole]], [[SystemRandomSource]]

## Usado por
[[MatchManager]], [[NetworkBombPoolHandler]], [[NetworkSpawnRegistry]], [[OnlineMatchBootstrap]]

## Eventos que escucha
[[Eventos GameEvents#OnMatchPhaseChanged|OnMatchPhaseChanged]]
