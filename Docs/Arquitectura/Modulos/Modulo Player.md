---
tags: [modulo, modulo/player]
---
# Módulo Player

Carpeta `Assets/Scripts/Player/` · 15 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class CharacterVisual {
    <<struct>>
    -CharacterDefinition _character
    -GameObject _root
    +CharacterDefinition Character
    +GameObject Root
  }
  class IPlayerRoster {
    <<interface>>
    +int JoinedCount
    +TryGetRig(PlayerId id, PlayerRig rig) bool
    +RespawnAll() void
    +SetControlsEnabled(bool enabled) void
  }
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
  class PlayerAppearance {
    <<MonoBehaviour>>
    -CharacterVisual[] _variants
    +ApplyCharacter(CharacterDefinition character) void
  }
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
  class PlayerJoinCoordinator {
    <<MonoBehaviour>>
    -PlayerInputManager _manager
    -Transform[] _spawnPoints
    -CharacterDefinition[] _fallbackCharacters
    -MatchSession _session
    -Camera _lobbyCamera
    +int JoinedCount
    +TryGetRig(PlayerId id, PlayerRig rig) bool
    +RespawnAll() void
    +SetControlsEnabled(bool enabled) void
  }
  class PlayerMenuRouter {
    <<MonoBehaviour>>
    -PlayerJoinCoordinator _players
    +PlayerId MenuOwner
    +RouteMenu(PlayerId owner, GameObject root, GameObject firstSelected) bool
    +ReleaseMenus() void
    +SetPlayersPaused(bool paused) void
  }
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
    ...10 más
  }
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
  class PlayerStunIndicator {
    <<MonoBehaviour>>
    -PlayerMovement _movement
    -Transform _indicator
    -float _spinSpeed
  }
  class PlayerStunReceiver {
    <<MonoBehaviour>>
    -PlayerSettings _settings
    -PlayerIdentity _identity
    -PlayerMovement _movement
    -PlayerGrabber _grabber
    +PlayerId Id
    +SimulationRole Role
    +float StunRemaining
    +SetSimulationRole(SimulationRole role) void
    +ReceiveStun(PlayerId source, Vector3 origin, float duration, float knockbackSpeed) void
  }
  class PlayerTackle {
    <<MonoBehaviour>>
    -PlayerSettings _settings
    -PlayerMovement _movement
    -PlayerGrabber _grabber
    -PlayerIdentity _identity
    +PlayerId Id
    +bool CanBeTackled
    +ReceiveTackle(Vector3 planarDirection) void
  }
  class ReleaseMode {
    <<enumeration>>
    Auto
    External
  }
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
  class ThrowTrajectoryView {
    <<MonoBehaviour>>
    -PlayerAim _aim
    -LineRenderer _line
    -Transform _impactMarker
    -LayerMask _collisionMask
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  class IGrabbable:::external
  <<interface>> IGrabbable
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class IStunnable:::external
  <<interface>> IStunnable
  class IThrowable:::external
  <<interface>> IThrowable
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  class PlayerMenuInput:::external
  <<MonoBehaviour>> PlayerMenuInput
  class PlayerSettings:::external
  <<ScriptableObject>> PlayerSettings
  class ThrowSettings:::external
  <<ScriptableObject>> ThrowSettings
  CharacterVisual --> CharacterDefinition
  PlayerAim --> IGrabbable
  PlayerAim --> IPlayerCommandSource
  PlayerAim --> IThrowable
  PlayerAim --> PlayerGrabber
  PlayerAim --> PlayerMovement
  PlayerAim --> ThirdPersonCamera
  PlayerAim --> ThrowSettings
  PlayerAppearance --> CharacterVisual
  PlayerGrabber --> IGrabbable
  PlayerGrabber --> IPlayerCommandSource
  PlayerGrabber --> PlayerAim
  PlayerGrabber --> PlayerIdentity
  PlayerGrabber --> PlayerMovement
  PlayerGrabber --> PlayerSettings
  PlayerGrabber ..|> ISimulationRoleTarget
  PlayerJoinCoordinator --> CharacterDefinition
  PlayerJoinCoordinator --> MatchSession
  PlayerJoinCoordinator --> PlayerRig
  PlayerJoinCoordinator ..|> IPlayerRoster
  PlayerMenuRouter --> PlayerJoinCoordinator
  PlayerMovement --> IPlayerCommandSource
  PlayerMovement --> PlayerSettings
  PlayerMovement ..|> ISimulationRoleTarget
  PlayerRig --> PlayerAim
  PlayerRig --> PlayerAppearance
  PlayerRig --> PlayerGrabber
  PlayerRig --> PlayerIdentity
  PlayerRig --> PlayerMenuInput
  PlayerRig --> PlayerMovement
  PlayerRig --> PlayerStunReceiver
  PlayerRig --> PlayerTackle
  PlayerRig --> ThirdPersonCamera
  PlayerStunIndicator --> PlayerMovement
  PlayerStunReceiver --> PlayerGrabber
  PlayerStunReceiver --> PlayerIdentity
  PlayerStunReceiver --> PlayerMovement
  PlayerStunReceiver --> PlayerSettings
  PlayerStunReceiver ..|> ISimulationRoleTarget
  PlayerStunReceiver ..|> IStunnable
  PlayerTackle --> PlayerGrabber
  PlayerTackle --> PlayerIdentity
  PlayerTackle --> PlayerMovement
  PlayerTackle --> PlayerSettings
  ThirdPersonCamera --> IPlayerCommandSource
  ThirdPersonCamera --> PlayerSettings
  ThrowTrajectoryView --> PlayerAim
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[CharacterVisual]] | struct | Pairs a character with its pre-built visual root inside the Player prefab. |
| [[IPlayerRoster]] | interface | Registry of the players in the match, as the match drives them. Local: `PlayerJoinCoordinator`; online, a network roster on the server. |
| [[PlayerAim]] | MonoBehaviour | Hold-to-aim for throws. Aiming is live: Aim held, grabbing enabled, not stunned and carrying something that can be thrown. While aiming, mov… |
| [[PlayerAppearance]] | MonoBehaviour | Applies the player's character visual by activating one of the pre-built variants under the Visual child (no instantiation) and deactivating… |
| [[PlayerGrabber]] | MonoBehaviour | Hold-to-carry grabbing: while Grab is held with empty hands, picks the closest `IGrabbable` in a cone in front of the player (also catching … |
| [[PlayerJoinCoordinator]] | MonoBehaviour | Scene composition root for local multiplayer joining: listens to `PlayerInputManager` and configures each spawned player (identity, spawn po… |
| [[PlayerMenuRouter]] | MonoBehaviour | Routes a shared menu (pause, results) to a single owner: the owner navigates it with its own event system while every other joined player is… |
| [[PlayerMovement]] | MonoBehaviour | Camera-relative, Rigidbody-driven ground movement with walk/sprint speeds. The root never rotates; only `_visual` turns to face the move dir… |
| [[PlayerRig]] | MonoBehaviour | Facade on the player root that groups the per-player components, so scene composition roots configure a spawned player through a single refe… |
| [[PlayerStunIndicator]] | MonoBehaviour | Shows a spinning ring of stars over the player while it is stunned (blast or tackle). |
| [[PlayerStunReceiver]] | MonoBehaviour | Blast target on the player root: drops whatever is carried (thrown away from the blast), knocks the player back and stuns it. Stuns never st… |
| [[PlayerTackle]] | MonoBehaviour | Stealing by tackle: a sprinting player that runs into a carrier (fast enough and facing it) makes the carrier drop its animal, get knocked b… |
| [[ReleaseMode]] | enumeration | Who decides when a `PlayerGrabber` lets go of what it carries. |
| [[ThirdPersonCamera]] | MonoBehaviour | Orbit camera rig driven by this player's Look input. Lives on the CameraRig (child of the non-rotating player root); the camera is a child p… |
| [[ThrowTrajectoryView]] | MonoBehaviour | Draws the throw arc while aiming. Simulates with the same integrator and step as PhysX (`BallisticMath.Step`, Time.fixedDeltaTime, Physics.g… |

## Depende de
[[Modulo Animals|Animals]] (3), [[Modulo Core|Core]] (38), [[Modulo Data|Data]] (13), [[Modulo Input|Input]] (2), [[Modulo Items|Items]] (1), [[Modulo Utilities|Utilities]] (3)