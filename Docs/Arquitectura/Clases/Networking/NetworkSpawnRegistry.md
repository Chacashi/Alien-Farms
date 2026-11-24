---
modulo: Networking
namespace: AlienFarms.Networking.Pooling
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Pooling/NetworkSpawnRegistry.cs"
tags: [clase, modulo/networking]
---
# NetworkSpawnRegistry

> Network pooling of animals and bombs (online gameplay scene, under OnlineModeRoot). Every instance: registers an `INetworkPrefabInstanceHandler` per animal prefab and for the bomb in Awake, before any spawn of this scene can arrive (NGO defers them until the scene finished loading), and removes them on destroy. Server: is the spawners' network hook. A simulation spawn becomes NetworkObject.Spawn (replicated state reset first); a despawn becomes Despawn(false) and the instance goes straight back to its pool. Exposes the pool counters (active, inactive, created) for the debug overlay.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Pooling` · **Archivo:** `Assets/Scripts/Networking/Pooling/NetworkSpawnRegistry.cs`

## Hereda / implementa
`MonoBehaviour`, [[IAnimalNetworkHook]], [[IBombNetworkHook]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkSpawnRegistry {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -AnimalSpawnSettings _animalSettings
    -BombData _bombData
    -AnimalSpawner _animalSpawner
    -BombSpawner _bombSpawner
    +NetworkPoolStats AnimalStats
    +NetworkPoolStats BombStats
  }
  class AnimalSpawnSettings:::external
  <<ScriptableObject>> AnimalSpawnSettings
  class AnimalSpawner:::external
  <<MonoBehaviour>> AnimalSpawner
  class BombData:::external
  <<ScriptableObject>> BombData
  class BombSpawner:::external
  <<MonoBehaviour>> BombSpawner
  class IAnimalNetworkHook:::external
  <<interface>> IAnimalNetworkHook
  class IBombNetworkHook:::external
  <<interface>> IBombNetworkHook
  class NetworkPoolLog:::external
  class NetworkPoolStats:::external
  <<struct>> NetworkPoolStats
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  NetworkSpawnRegistry --> AnimalSpawnSettings
  NetworkSpawnRegistry --> AnimalSpawner
  NetworkSpawnRegistry --> BombData
  NetworkSpawnRegistry --> BombSpawner
  NetworkSpawnRegistry --> NetworkPoolLog
  NetworkSpawnRegistry --> NetworkPoolStats
  NetworkSpawnRegistry --> NetworkRuntimeAnchor
  NetworkSpawnRegistry ..|> IAnimalNetworkHook
  NetworkSpawnRegistry ..|> IBombNetworkHook
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[AnimalData]], [[AnimalSpawnEntry]], [[AnimalSpawnSettings]], [[AnimalSpawner]], [[AnimalTypePool]], [[Bomb]], [[BombData]], [[BombSpawner]], [[IAnimalNetworkHook]], [[IBombNetworkHook]], [[NetworkAnimal]], [[NetworkAnimalPoolHandler]], [[NetworkBomb]], [[NetworkBombPoolHandler]], [[NetworkCarriable]], [[NetworkPoolLog]], [[NetworkPoolStats]], [[NetworkRuntimeAnchor]], [[PooledParenting]]

## Usado por
[[NetworkPoolLog]], [[PoolStatsView]]
