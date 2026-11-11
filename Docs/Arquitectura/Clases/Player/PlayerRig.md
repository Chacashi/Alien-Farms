---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/PlayerRig.cs"
tags: [clase, modulo/player]
---
# PlayerRig

> Facade on the player root that groups the per-player components, so scene composition roots configure a spawned player through a single reference.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/PlayerRig.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerRig {
    <<MonoBehaviour>>
    -PlayerIdentity _identity
    -PlayerMovement _movement
    -ThirdPersonCamera _camera
    -PlayerAppearance _appearance
    -PlayerGrabber _grabber
    -PlayerMenuInput _menu
    -PlayerAim _aim
    -PlayerTackle _tackle
    -PlayerStunReceiver _stunReceiver
    +PlayerId Id
    +PlayerGrabber Grabber
    +PlayerMenuInput Menu
    +PlayerMovement Movement
    ...8 más
  }
  class PlayerAim:::external
  <<MonoBehaviour>> PlayerAim
  class PlayerAppearance:::external
  <<MonoBehaviour>> PlayerAppearance
  class PlayerGrabber:::external
  <<MonoBehaviour>> PlayerGrabber
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  class PlayerMenuInput:::external
  <<MonoBehaviour>> PlayerMenuInput
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  class PlayerStunReceiver:::external
  <<MonoBehaviour>> PlayerStunReceiver
  class PlayerTackle:::external
  <<MonoBehaviour>> PlayerTackle
  class ThirdPersonCamera:::external
  <<MonoBehaviour>> ThirdPersonCamera
  PlayerRig --> PlayerAim
  PlayerRig --> PlayerAppearance
  PlayerRig --> PlayerGrabber
  PlayerRig --> PlayerIdentity
  PlayerRig --> PlayerMenuInput
  PlayerRig --> PlayerMovement
  PlayerRig --> PlayerStunReceiver
  PlayerRig --> PlayerTackle
  PlayerRig --> ThirdPersonCamera
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[DropReason]], [[PlayerAim]], [[PlayerAppearance]], [[PlayerGrabber]], [[PlayerId]], [[PlayerIdentity]], [[PlayerMenuInput]], [[PlayerMovement]], [[PlayerStunReceiver]], [[PlayerTackle]], [[ThirdPersonCamera]]

## Usado por
[[IPlayerRoster]], [[PlayerJoinCoordinator]], [[PlayerMenuRouter]]
