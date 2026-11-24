---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Session/NetworkSceneLoader.cs"
tags: [clase, modulo/networking]
---
# NetworkSceneLoader

> Server-side scene loading through NGO scene management: the host loads the gameplay scene (Single) and every client, including late joiners, follows on synchronization. Raises `LoadCompleted` once every client finished (or timed out).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/NetworkSceneLoader.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkSceneLoader {
    <<MonoBehaviour>>
    -NetworkManager _manager
    +bool IsLoading
    +event Action~string~ LoadCompleted
    +LoadGameplay() bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[SceneNames]]

## Usado por
[[DevConnectionPanel]], [[LobbyStateSpawner]], [[NetworkLobbyState]], [[NetworkPlayerRoster]], [[NetworkRootLifetime]], [[NetworkRuntimeAnchor]], [[Status]]
