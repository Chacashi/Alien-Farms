---
modulo: Player
namespace: AlienFarms.Player
tipo: interface
archivo: "Assets/Scripts/Player/IPlayerRigLookup.cs"
tags: [clase, modulo/player]
---
# IPlayerRigLookup

> Finds the rig of a player. What shared menus (pause, results) need: locally every joined player; online only the avatar this machine owns.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/IPlayerRigLookup.cs`

## Derivados / implementaciones
[[IPlayerRoster]], [[LocalPlayerRigLookup]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IPlayerRigLookup {
    <<interface>>
    +TryGetRig(PlayerId id, PlayerRig rig) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]], [[PlayerRig]]

## Usado por
[[GameModeRouter]], [[IPlayerRoster]], [[LocalPlayerRigLookup]], [[PlayerMenuRouter]]
