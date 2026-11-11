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

## Hereda / implementa
[[IPlayerRigLookup]]

## Derivados / implementaciones
[[NetworkPlayerRoster]], [[PlayerJoinCoordinator]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IPlayerRoster {
    <<interface>>
    +int JoinedCount
    +RespawnAll() void
    +SetControlsEnabled(bool enabled) void
  }
  class IPlayerRigLookup:::external
  <<interface>> IPlayerRigLookup
  IPlayerRoster ..|> IPlayerRigLookup
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IPlayerRigLookup]]

## Usado por
[[GameModeRouter]], [[MatchManager]], [[NetworkPlayerRoster]], [[PlayerJoinCoordinator]]
