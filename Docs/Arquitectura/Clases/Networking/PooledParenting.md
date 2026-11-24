---
modulo: Networking
namespace: AlienFarms.Networking.Pooling
tipo: static
archivo: "Assets/Scripts/Networking/Pooling/PooledParenting.cs"
tags: [clase, modulo/networking]
---
# PooledParenting

> Lets a pool put a despawned `NetworkObject` back under its (non-network) container. NGO reverts and logs an error for any parent change of a NetworkObject that is not spawned, so parent sync is switched off for the duration of the release only. Call `BeginRelease` / `EndRelease` around the pool's Release, always after Despawn.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Pooling` · **Archivo:** `Assets/Scripts/Networking/Pooling/PooledParenting.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PooledParenting {
    <<static>>
    +BeginRelease(NetworkObject networkObject, bool previousSync)$ void
    +EndRelease(NetworkObject networkObject, bool previousSync)$ void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[NetworkAnimalPoolHandler]], [[NetworkBombPoolHandler]], [[NetworkSpawnRegistry]]
