---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/PlayerJoinCoordinator.cs"
tags: [clase, modulo/player]
---
# PlayerJoinCoordinator

> Scene composition root for local multiplayer joining: listens to `PlayerInputManager` and configures each spawned player (identity, spawn point, camera yaw, character) through its `PlayerRig`. Keeps a registry of joined rigs for the match (count, respawn, freeze).

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/PlayerJoinCoordinator.cs`

## Hereda / implementa
`MonoBehaviour`, [[IPlayerRoster]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerJoinCoordinator {
    <<MonoBehaviour>>
    -PlayerInputManager _manager
    -Transform[] _spawnPoints
    -CharacterDefinition[] _fallbackCharacters
    -MatchSession _session
    -Camera _lobbyCamera
    +int JoinedCount
    +TryGetRig(PlayerId id, PlayerRig rig) bool
    +RespawnAll() void
    +SetControlsEnabled(bool enabled) void
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  class IPlayerRoster:::external
  <<interface>> IPlayerRoster
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  class PlayerRig:::external
  <<MonoBehaviour>> PlayerRig
  PlayerJoinCoordinator --> CharacterDefinition
  PlayerJoinCoordinator --> MatchSession
  PlayerJoinCoordinator --> PlayerRig
  PlayerJoinCoordinator ..|> IPlayerRoster
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[GameEvents]], [[IPlayerRoster]], [[MatchSession]], [[PlayerId]], [[PlayerRig]], [[SessionSlot]]

## Usado por
[[MatchManager]], [[PlayerMenuRouter]]

## Eventos que publica
[[Eventos GameEvents#OnPlayerCharacterAssigned|OnPlayerCharacterAssigned]], [[Eventos GameEvents#OnPlayerJoined|OnPlayerJoined]], [[Eventos GameEvents#OnPlayerLeft|OnPlayerLeft]]
