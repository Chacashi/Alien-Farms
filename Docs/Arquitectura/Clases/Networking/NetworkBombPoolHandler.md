---
modulo: Networking
namespace: AlienFarms.Networking.Pooling
tipo: class
archivo: "Assets/Scripts/Networking/Pooling/NetworkBombPoolHandler.cs"
tags: [clase, modulo/networking]
---
# NetworkBombPoolHandler

> NGO instance handler for the bomb prefab, on top of the `BombSpawner` pool. Clients: a spawn takes a pooled Replica (`BombSpawner.GetForReplica`), a despawn returns it (`Bomb.Despawn` as Replica). On the server NGO only calls Destroy while shutting down: `NetworkBomb` has already switched a bomb still in play to Replica in OnNetworkDespawn, so it goes back to the pool too; an Authority one is left alone.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Pooling` · **Archivo:** `Assets/Scripts/Networking/Pooling/NetworkBombPoolHandler.cs`

## Hereda / implementa
`INetworkPrefabInstanceHandler`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkBombPoolHandler {
    +Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) NetworkObject
    +Destroy(NetworkObject networkObject) void
  }
  class BombSpawner:::external
  <<MonoBehaviour>> BombSpawner
  class NetworkPoolLog:::external
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  NetworkBombPoolHandler --> BombSpawner
  NetworkBombPoolHandler --> NetworkPoolLog
  NetworkBombPoolHandler --> NetworkRuntimeAnchor
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Bomb]], [[BombSpawner]], [[NetworkBomb]], [[NetworkPoolLog]], [[NetworkRuntimeAnchor]], [[PooledParenting]], [[SimulationRole]]

## Usado por
[[NetworkSpawnRegistry]]
