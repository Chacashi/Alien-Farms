---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/PlayerAim.cs"
tags: [clase, modulo/player]
---
# PlayerAim

> Hold-to-aim for throws. Aiming is live: Aim held, grabbing enabled, not stunned and carrying something that can be thrown. While aiming, movement strafes facing the camera and the camera zooms in. The launch comes from the camera: planar forward for direction, pitch for elevation.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/PlayerAim.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerAim {
    <<MonoBehaviour>>
    -ThrowSettings _defaultThrowSettings
    -MonoBehaviour _input
    -PlayerGrabber _grabber
    -PlayerMovement _movement
    -ThirdPersonCamera _camera
    +bool IsAiming
    +IThrowable HeldThrowable
    +ThrowSettings ActiveThrowSettings
    +SetCommandSource(IPlayerCommandSource source) void
    +TryGetLaunch(Vector3 origin, Vector3 velocity) bool
    +TryGetLaunchWithView(float yaw, float pitch, Vector3 origin, Vector3 velocity) bool
  }
  class IGrabbable:::external
  <<interface>> IGrabbable
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  class IThrowable:::external
  <<interface>> IThrowable
  class PlayerGrabber:::external
  <<MonoBehaviour>> PlayerGrabber
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  class ThirdPersonCamera:::external
  <<MonoBehaviour>> ThirdPersonCamera
  class ThrowSettings:::external
  <<ScriptableObject>> ThrowSettings
  PlayerAim --> IGrabbable
  PlayerAim --> IPlayerCommandSource
  PlayerAim --> IThrowable
  PlayerAim --> PlayerGrabber
  PlayerAim --> PlayerMovement
  PlayerAim --> ThirdPersonCamera
  PlayerAim --> ThrowSettings
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[BallisticMath]], [[IGrabbable]], [[IPlayerCommandSource]], [[IThrowable]], [[LayerNames]], [[PlayerGrabber]], [[PlayerMovement]], [[ThirdPersonCamera]], [[ThrowSettings]]

## Usado por
[[Mode]], [[NetworkCarry]], [[NetworkPlayerInput]], [[PlayerGrabber]], [[PlayerRig]], [[ThrowTrajectoryView]]
