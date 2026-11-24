---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: class
archivo: "Assets/Scripts/Networking/Session/NetworkLobbyState.cs"
tags: [clase, modulo/networking]
---
# NetworkLobbyState

> Networked online room (prefab LobbyState, spawned in MainMenu by `LobbyStateSpawner` on the host). The server owns the seats and picks (`LobbyRules` on a 2-entry mirror) and starts the match; every peer republishes the replicated state into the `OnlineLobbyChannel` the menu reads, and forwards the menu's character requests to the server. Everything is event driven: no per-frame work.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/NetworkLobbyState.cs`

## Hereda / implementa
`NetworkBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkLobbyState {
    -NetworkRuntimeAnchor _anchor
    -NetworkSettings _settings
    -CharacterRoster _roster
    -OnlineLobbyChannel _lobbyChannel
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
    +OnDestroy() void
  }
  class CharacterRoster:::external
  <<ScriptableObject>> CharacterRoster
  class LobbySlotEntry:::external
  <<struct>> LobbySlotEntry
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class OnlineLobbyChannel:::external
  <<ScriptableObject>> OnlineLobbyChannel
  class OnlineLobbySeat:::external
  <<struct>> OnlineLobbySeat
  class OnlineSessionChannel:::external
  <<ScriptableObject>> OnlineSessionChannel
  class PlayerSlot:::external
  <<struct>> PlayerSlot
  NetworkLobbyState --> CharacterRoster
  NetworkLobbyState --> LobbySlotEntry
  NetworkLobbyState --> NetworkRuntimeAnchor
  NetworkLobbyState --> NetworkSettings
  NetworkLobbyState --> OnlineLobbyChannel
  NetworkLobbyState --> OnlineLobbySeat
  NetworkLobbyState --> OnlineSessionChannel
  NetworkLobbyState --> PlayerSlot
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterRoster]], [[DisplayNameSanitizer]], [[LobbyRequestResult]], [[LobbyRules]], [[LobbySlotEntry]], [[NetworkRuntimeAnchor]], [[NetworkSceneLoader]], [[NetworkSettings]], [[OnlineLobbyChannel]], [[OnlineLobbyPhase]], [[OnlineLobbySeat]], [[OnlineSessionChannel]], [[OnlineSessionErrorKeys]], [[PlayerId]], [[PlayerSlot]], [[ServerSlotDirectory]]
