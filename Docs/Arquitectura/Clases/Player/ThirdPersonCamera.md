---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/ThirdPersonCamera.cs"
tags: [clase, modulo/player]
---
# ThirdPersonCamera

> Orbit camera rig driven by this player's Look input. Lives on the CameraRig (child of the non-rotating player root); the camera is a child placed at local (0, 0, -distance). While aiming, the distance blends to the aim distance and look sensitivity is reduced. In driven mode (`SetDrivenView`) yaw and pitch come from outside and the Look input is ignored.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/ThirdPersonCamera.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ThirdPersonCamera {
    <<MonoBehaviour>>
    -PlayerSettings _settings
    -MonoBehaviour _input
    -Transform _target
    -Transform _cameraTransform
    -LayerMask _collisionMask
    +float Pitch
    +float Yaw
    +Vector3 PlanarForward
    +bool IsAiming
    +bool IsDriven
    +SetCommandSource(IPlayerCommandSource source) void
    +SetDrivenView(float yaw, float pitch) void
    +ReleaseDrivenView() void
    ...2 más
  }
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  class PlayerSettings:::external
  <<ScriptableObject>> PlayerSettings
  ThirdPersonCamera --> IPlayerCommandSource
  ThirdPersonCamera --> PlayerSettings
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[IPlayerCommandSource]], [[LayerNames]], [[PlayerSettings]]

## Usado por
[[Mode]], [[NetworkCarry]], [[NetworkPlayer]], [[NetworkPlayerInput]], [[PlayerAim]], [[PlayerRig]]
