---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/PlayerGrabber.cs"
tags: [clase, modulo/player]
---
# PlayerGrabber

> Hold-to-carry grabbing: while Grab is held with empty hands, picks the closest `IGrabbable` in a cone in front of the player (also catching ones that walk into range, free or penned). Releasing Grab drops it in front, or throws it along the aimed arc while `PlayerAim` is aiming. Carries at most one object. With `ReleaseMode.External` the release is decided from outside (`ReleaseHeld`, `ThrowWithView`) instead of by the Grab button. As a `SimulationRole.Replica` it never grabs, drops or throws: it only mirrors the held object set with `SetReplicatedHeld`.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/PlayerGrabber.cs`

## Hereda / implementa
`MonoBehaviour`, [[ISimulationRoleTarget]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerGrabber {
    <<MonoBehaviour>>
    -PlayerSettings _settings
    -MonoBehaviour _input
    -PlayerIdentity _identity
    -PlayerMovement _movement
    -Transform _holdPoint
    -PlayerAim _aim
    -LayerMask _grabbableMask
    +bool IsHolding
    +IGrabbable Held
    +bool IsInputEnabled
    +ReleaseMode ReleaseMode
    +SimulationRole Role
    +ForceDrop(DropReason reason, Vector3 extraVelocity) void
    ...8 más
  }
  class IGrabbable:::external
  <<interface>> IGrabbable
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class PlayerAim:::external
  <<MonoBehaviour>> PlayerAim
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  class PlayerSettings:::external
  <<ScriptableObject>> PlayerSettings
  PlayerGrabber --> IGrabbable
  PlayerGrabber --> IPlayerCommandSource
  PlayerGrabber --> PlayerAim
  PlayerGrabber --> PlayerIdentity
  PlayerGrabber --> PlayerMovement
  PlayerGrabber --> PlayerSettings
  PlayerGrabber ..|> ISimulationRoleTarget
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[Bomb]], [[DropReason]], [[IGrabbable]], [[IPlayerCommandSource]], [[ISimulationRoleTarget]], [[LayerNames]], [[PlayerAim]], [[PlayerId]], [[PlayerIdentity]], [[PlayerMovement]], [[PlayerSettings]], [[ReleaseMode]], [[SimulationRole]]

## Usado por
[[Mode]], [[NetworkPlayer]], [[NetworkPlayerInput]], [[PlayerAim]], [[PlayerRig]], [[PlayerStunReceiver]], [[PlayerTackle]]
