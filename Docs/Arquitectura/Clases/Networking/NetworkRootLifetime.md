---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Session/NetworkRootLifetime.cs"
tags: [clase, modulo/networking]
---
# NetworkRootLifetime

> Lifetime of the persistent NetworkRoot: the first instance survives scene loads and publishes its services on the `NetworkRuntimeAnchor`; any later copy (MainMenu reloaded) deactivates and destroys itself in Awake, before its NetworkManager can claim the singleton. `Teardown` ends the session and removes the root. Runs first so the duplicate guard wins over NetworkManager.OnEnable. A host with remote clients shuts down gracefully (`ShutdownSessionAsync`): it sends the leaving notice, keeps the connections open for `NetworkSettings.HostLeaveNoticeSeconds`, disconnects the clients with `DisconnectReasons.HostDisconnected` and only then stops NGO (see `HostShutdownSequence`). Meanwhile a teardown keeps the root alive (unpublished, in DontDestroyOnLoad) and runs the caller's continuation (loading the next scene) once NGO stopped. Anything else shuts down at once.

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
    -NetworkSettings _settings
    -NetworkManager _manager
    -ConnectionApprovalHandler _approval
    -NetworkSceneLoader _sceneLoader
    -MonoBehaviour _connection
    -LeaveNoticeMessenger _leaveNotice
    -OnlineSessionService _sessionService
    +bool IsShutdownPending
    +event Action HostLeaveStarted
    +NetworkManager Manager
    +ConnectionApprovalHandler Approval
    +NetworkSceneLoader SceneLoader
    ...8 más
  }
  class ConnectionApprovalHandler:::external
  <<MonoBehaviour>> ConnectionApprovalHandler
  class HostShutdownSequence:::external
  class IConnectionStrategy:::external
  <<interface>> IConnectionStrategy
  class LeaveNoticeMessenger:::external
  <<MonoBehaviour>> LeaveNoticeMessenger
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  class NetworkSceneLoader:::external
  <<MonoBehaviour>> NetworkSceneLoader
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class OnlineSessionService:::external
  <<MonoBehaviour>> OnlineSessionService
  NetworkRootLifetime --> ConnectionApprovalHandler
  NetworkRootLifetime --> HostShutdownSequence
  NetworkRootLifetime --> IConnectionStrategy
  NetworkRootLifetime --> LeaveNoticeMessenger
  NetworkRootLifetime --> NetworkRuntimeAnchor
  NetworkRootLifetime --> NetworkSceneLoader
  NetworkRootLifetime --> NetworkSettings
  NetworkRootLifetime --> OnlineSessionService
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ConnectionApprovalHandler]], [[DisconnectReasons]], [[HostShutdownSequence]], [[HostShutdownStep]], [[IConnectionStrategy]], [[LeaveNoticeMessenger]], [[NetworkRuntimeAnchor]], [[NetworkSceneLoader]], [[NetworkSettings]], [[OnlineSessionService]]

## Usado por
[[DevConnectionPanel]], [[NetworkRuntimeAnchor]], [[OnlineMatchBootstrap]], [[OnlineMatchExitHandler]], [[OnlineMatchStatsRecorder]], [[OnlineSessionService]], [[ReconnectGraceCoordinator]], [[SessionExit]], [[Status]]
