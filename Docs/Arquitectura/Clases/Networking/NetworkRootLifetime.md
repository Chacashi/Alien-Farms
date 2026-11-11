---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Session/NetworkRootLifetime.cs"
tags: [clase, modulo/networking]
---
# NetworkRootLifetime

> Lifetime of the persistent NetworkRoot: the first instance survives scene loads and publishes its services on the `NetworkRuntimeAnchor`; any later copy (MainMenu reloaded) deactivates and destroys itself in Awake, before its NetworkManager can claim the singleton. `Teardown` ends the session and removes the root. Runs first so the duplicate guard wins over NetworkManager.OnEnable.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/NetworkRootLifetime.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkRootLifetime {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -NetworkManager _manager
    -ConnectionApprovalHandler _approval
    -NetworkSceneLoader _sceneLoader
    -MonoBehaviour _connection
    +NetworkManager Manager
    +ConnectionApprovalHandler Approval
    +NetworkSceneLoader SceneLoader
    +IConnectionStrategy Connection
    +Teardown() void
    +ShutdownSession() void
  }
  class ConnectionApprovalHandler:::external
  <<MonoBehaviour>> ConnectionApprovalHandler
  class IConnectionStrategy:::external
  <<interface>> IConnectionStrategy
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  class NetworkSceneLoader:::external
  <<MonoBehaviour>> NetworkSceneLoader
  NetworkRootLifetime --> ConnectionApprovalHandler
  NetworkRootLifetime --> IConnectionStrategy
  NetworkRootLifetime --> NetworkRuntimeAnchor
  NetworkRootLifetime --> NetworkSceneLoader
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ConnectionApprovalHandler]], [[DisconnectReasons]], [[IConnectionStrategy]], [[NetworkRuntimeAnchor]], [[NetworkSceneLoader]]

## Usado por
[[DevConnectionPanel]], [[NetworkRuntimeAnchor]], [[OnlineMatchBootstrap]], [[OnlineMatchExitHandler]], [[Status]]
