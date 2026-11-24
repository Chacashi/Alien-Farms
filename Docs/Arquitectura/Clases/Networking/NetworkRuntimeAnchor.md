---
modulo: Networking
namespace: AlienFarms.Networking.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Networking/Data/NetworkRuntimeAnchor.cs"
tags: [clase, modulo/networking]
---
# NetworkRuntimeAnchor

> Runtime anchor: the persistent NetworkRoot publishes its services here so scene objects (roster, bootstrap, dev panel) reach them through a serialized asset reference instead of Find* or singletons. Nothing is serialized; references from a previous play session are discarded through a generation counter (no domain reload).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Data` · **Archivo:** `Assets/Scripts/Networking/Data/NetworkRuntimeAnchor.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkRuntimeAnchor {
    <<ScriptableObject>>
    +NetworkRootLifetime Root
    +bool HasRoot
    +NetworkManager Manager
    +ConnectionApprovalHandler Approval
    +ServerSlotDirectory Slots
    +NetworkSceneLoader SceneLoader
    +IConnectionStrategy Connection
    +NetworkPlayer LocalPlayer
    +Camera ViewCamera
    +PlayerStatsLedger PlayerStats
    +bool IsServer
    +bool IsPureClient
    +Publish(NetworkRootLifetime root) void
    ...12 más
  }
  class ConnectionApprovalHandler:::external
  <<MonoBehaviour>> ConnectionApprovalHandler
  class IConnectionStrategy:::external
  <<interface>> IConnectionStrategy
  class NetworkPlayer:::external
  class NetworkRootLifetime:::external
  <<MonoBehaviour>> NetworkRootLifetime
  class NetworkSceneLoader:::external
  <<MonoBehaviour>> NetworkSceneLoader
  class PlayerStatsLedger:::external
  class ServerSlotDirectory:::external
  NetworkRuntimeAnchor --> ConnectionApprovalHandler
  NetworkRuntimeAnchor --> IConnectionStrategy
  NetworkRuntimeAnchor --> NetworkPlayer
  NetworkRuntimeAnchor --> NetworkRootLifetime
  NetworkRuntimeAnchor --> NetworkSceneLoader
  NetworkRuntimeAnchor --> PlayerStatsLedger
  NetworkRuntimeAnchor --> ServerSlotDirectory
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ConnectionApprovalHandler]], [[IConnectionStrategy]], [[NetworkPlayer]], [[NetworkRootLifetime]], [[NetworkSceneLoader]], [[PlayerId]], [[PlayerPrefsPendingStatsStore]], [[PlayerStatsLedger]], [[ServerSlotDirectory]]

## Usado por
[[CloudSaveStatsService]], [[DevConnectionPanel]], [[LobbyStateSpawner]], [[LocalPlayerRigLookup]], [[NetworkAnimalPoolHandler]], [[NetworkBombPoolHandler]], [[NetworkCarriable]], [[NetworkLobbyState]], [[NetworkMatchExit]], [[NetworkNameTag]], [[NetworkPlayer]], [[NetworkPlayerRoster]], [[NetworkRootLifetime]], [[NetworkSpawnRegistry]], [[OnlineMatchBootstrap]], [[OnlineMatchExitHandler]], [[OnlineMatchStatsRecorder]], [[OnlineSessionService]], [[PoolStatsView]], [[ReconnectGraceCoordinator]], [[SessionExit]], [[Status]]
