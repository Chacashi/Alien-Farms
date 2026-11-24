---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: class
archivo: "Assets/Scripts/Networking/Player/NetworkPlayerInput.cs"
tags: [clase, modulo/networking]
---
# NetworkPlayerInput

> Input over the network, one command per physics step. Owner (pure client): samples its `PlayerInputHandler` and camera view and sends the last commands unreliably (redundant). Server, remote avatar: buffers them (`InputCommandBuffer`), applies one per step to a `NetworkCommandSource` injected into movement / grab / aim, and drives the camera view so movement is relative to the client's camera. The host's own avatar keeps its local handler. Runs before the gameplay components so they read this step's command.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/NetworkPlayerInput.cs`

## Hereda / implementa
`NetworkBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkPlayerInput {
    -NetworkSettings _settings
    -PlayerInputHandler _localInput
    -ThirdPersonCamera _camera
    -PlayerMovement _movement
    -PlayerGrabber _grabber
    -PlayerAim _aim
    +IPlayerCommandSource ServerCommands
    +bool IsRemotePaused
    +uint LastAppliedSeq
    +OnNetworkSpawn() void
    +OnNetworkDespawn() void
  }
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  class InputCommandBatch:::external
  <<struct>> InputCommandBatch
  class InputCommandBuffer:::external
  class NetworkCommandSource:::external
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class PlayerAim:::external
  <<MonoBehaviour>> PlayerAim
  class PlayerGrabber:::external
  <<MonoBehaviour>> PlayerGrabber
  class PlayerInputHandler:::external
  <<MonoBehaviour>> PlayerInputHandler
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  class ThirdPersonCamera:::external
  <<MonoBehaviour>> ThirdPersonCamera
  NetworkPlayerInput --> IPlayerCommandSource
  NetworkPlayerInput --> InputCommandBatch
  NetworkPlayerInput --> InputCommandBuffer
  NetworkPlayerInput --> NetworkCommandSource
  NetworkPlayerInput --> NetworkSettings
  NetworkPlayerInput --> PlayerAim
  NetworkPlayerInput --> PlayerGrabber
  NetworkPlayerInput --> PlayerInputHandler
  NetworkPlayerInput --> PlayerMovement
  NetworkPlayerInput --> ThirdPersonCamera
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IPlayerCommandSource]], [[InputCommandBatch]], [[InputCommandBuffer]], [[Mode]], [[NetworkCommandSource]], [[NetworkSettings]], [[PlayerAim]], [[PlayerGrabber]], [[PlayerInputCommand]], [[PlayerInputHandler]], [[PlayerMovement]], [[ThirdPersonCamera]]

## Usado por
[[Mode]], [[NetworkCarry]], [[NetworkPlayerMotion]]
