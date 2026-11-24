---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Session/LobbyStateSpawner.cs"
tags: [clase, modulo/networking]
---
# LobbyStateSpawner

> MainMenu-only: spawns the networked room (`NetworkLobbyState`) once the host is in a Sessions + Relay session. Lives in the scene and not on the NetworkRoot, so a direct host from the dev panel never creates a room. Never despawns it: NGO's shutdown or the room itself (when the match starts) does.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/LobbyStateSpawner.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LobbyStateSpawner {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -OnlineSessionChannel _session
    -OnlineLobbyChannel _lobbyChannel
    -NetworkObject _lobbyPrefab
  }
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  class OnlineLobbyChannel:::external
  <<ScriptableObject>> OnlineLobbyChannel
  class OnlineSessionChannel:::external
  <<ScriptableObject>> OnlineSessionChannel
  LobbyStateSpawner --> NetworkRuntimeAnchor
  LobbyStateSpawner --> OnlineLobbyChannel
  LobbyStateSpawner --> OnlineSessionChannel
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[NetworkRuntimeAnchor]], [[NetworkSceneLoader]], [[OnlineLobbyChannel]], [[OnlineLobbyPhase]], [[OnlineSessionChannel]], [[OnlineSessionStatus]], [[Status]]
