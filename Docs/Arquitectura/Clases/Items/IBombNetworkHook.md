---
modulo: Items
namespace: AlienFarms.Items
tipo: interface
archivo: "Assets/Scripts/Items/IBombNetworkHook.cs"
tags: [clase, modulo/items]
---
# IBombNetworkHook

> Lets a network layer follow the authority's bomb lifecycle. Without a hook, bombs go straight back to their pool on despawn (local mode).

**Módulo:** [[Modulo Items|Items]] · **Namespace:** `AlienFarms.Items` · **Archivo:** `Assets/Scripts/Items/IBombNetworkHook.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class IBombNetworkHook {
    <<interface>>
    +OnSpawned(Bomb bomb) void
    +TryDespawn(Bomb bomb) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Bomb]]

## Usado por
[[Bomb]], [[BombSpawner]]
