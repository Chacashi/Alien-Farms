---
modulo: Player
namespace: AlienFarms.Player
tipo: interface
archivo: "Assets/Scripts/Player/IPlayerRoster.cs"
tags: [clase, modulo/player]
---
# IPlayerRoster

> Registry of the players in the match, as the match drives them. Local: `PlayerJoinCoordinator`; online, a network roster on the server.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/IPlayerRoster.cs`

## Derivados / implementaciones
[[PlayerJoinCoordinator]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IPlayerRoster {
    <<interface>>
    +int JoinedCount
    +TryGetRig(PlayerId id, PlayerRig rig) bool
    +RespawnAll() void
    +SetControlsEnabled(bool enabled) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]], [[PlayerRig]]

## Usado por
[[MatchManager]], [[PlayerJoinCoordinator]]
