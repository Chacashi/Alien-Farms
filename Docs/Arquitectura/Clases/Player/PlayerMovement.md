---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/PlayerMovement.cs"
tags: [clase, modulo/player]
---
# PlayerMovement

> Camera-relative, Rigidbody-driven ground movement with walk/sprint speeds. The root never rotates; only `_visual` turns to face the move direction (or, while aiming, the camera's planar forward so the player strafes). Input comes from an `IPlayerCommandSource`. As a `SimulationRole.Replica` it does not move.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/PlayerMovement.cs`

## Hereda / implementa
`MonoBehaviour`, [[ISimulationRoleTarget]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerMovement {
    <<MonoBehaviour>>
    -PlayerSettings _settings
    -MonoBehaviour _input
    -Rigidbody _rb
    -Transform _cameraYawSource
    -Transform _visual
    +Vector3 Facing
    +bool IsSprinting
    +bool IsStunned
    +bool IsCarrying
    +bool IsInputEnabled
    +bool IsAiming
    +Vector3 Velocity
    +float StunRemaining
    ...11 más
  }
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class PlayerSettings:::external
  <<ScriptableObject>> PlayerSettings
  PlayerMovement --> IPlayerCommandSource
  PlayerMovement --> PlayerSettings
  PlayerMovement ..|> ISimulationRoleTarget
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IPlayerCommandSource]], [[ISimulationRoleTarget]], [[PlayerSettings]], [[SimulationRole]]

## Usado por
[[Mode]], [[NetworkPlayer]], [[NetworkPlayerInput]], [[NetworkPlayerMotion]], [[PlayerAim]], [[PlayerGrabber]], [[PlayerRig]], [[PlayerStunIndicator]], [[PlayerStunReceiver]], [[PlayerTackle]]
