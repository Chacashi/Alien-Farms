---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: class
archivo: "Assets/Scripts/Networking/Player/NetworkPlayer.cs"
tags: [clase, modulo/networking]
---
# NetworkPlayer

> Network identity of a Player_Net avatar. The roster hands PlayerId, character and name over before spawning and the server writes them in OnNetworkSpawn (NetworkVariables cannot be written earlier); every instance applies them to `PlayerIdentity` / `PlayerAppearance`. Only the owner enables input, UI input, camera (full screen) and audio listener, and registers itself as the anchor's local player. The orbit camera rig runs on the owner and, on the server, on remote avatars (driven by the client's view). Off the server the simulation runs as Replica (no prediction yet). On pure clients this adapter raises the join/character/leave `GameEvents`; on the server the roster does (no double events on the host).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/NetworkPlayer.cs`

## Hereda / implementa
`NetworkBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkPlayer {
    -CharacterRoster _characters
    -NetworkRuntimeAnchor _anchor
    -PlayerRig _rig
    -PlayerIdentity _identity
    -PlayerAppearance _appearance
    -PlayerMovement _movement
    -PlayerGrabber _grabber
    -PlayerStunReceiver _stunReceiver
    -NetworkPlayerMotion _motion
    -NetworkCarry _carry
    -PlayerInput _playerInput
    -Camera _camera
    -AudioListener _audioListener
    ...17 más
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  class CharacterRoster:::external
  <<ScriptableObject>> CharacterRoster
  class NetworkCarry:::external
  class NetworkPlayerMotion:::external
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  class PlayerAppearance:::external
  <<MonoBehaviour>> PlayerAppearance
  class PlayerGrabber:::external
  <<MonoBehaviour>> PlayerGrabber
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  class PlayerRig:::external
  <<MonoBehaviour>> PlayerRig
  class PlayerStunReceiver:::external
  <<MonoBehaviour>> PlayerStunReceiver
  NetworkPlayer --> CharacterDefinition
  NetworkPlayer --> CharacterRoster
  NetworkPlayer --> NetworkCarry
  NetworkPlayer --> NetworkPlayerMotion
  NetworkPlayer --> NetworkRuntimeAnchor
  NetworkPlayer --> PlayerAppearance
  NetworkPlayer --> PlayerGrabber
  NetworkPlayer --> PlayerIdentity
  NetworkPlayer --> PlayerMovement
  NetworkPlayer --> PlayerRig
  NetworkPlayer --> PlayerStunReceiver
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[CharacterRoster]], [[DisplayNameSanitizer]], [[GameEvents]], [[MatchPhase]], [[NetworkCarry]], [[NetworkPlayerMotion]], [[NetworkRuntimeAnchor]], [[PlayerAppearance]], [[PlayerGrabber]], [[PlayerId]], [[PlayerIdentity]], [[PlayerMovement]], [[PlayerRig]], [[PlayerSlot]], [[PlayerStunReceiver]], [[SimulationRole]], [[ThirdPersonCamera]]

## Usado por
[[CarriableOwnerTint]], [[LocalPlayerRigLookup]], [[NetworkCarriable]], [[NetworkMatchExit]], [[NetworkNameTag]], [[NetworkPlayerRoster]], [[NetworkRuntimeAnchor]], [[OnlineMatchExitHandler]], [[OnlineMatchStatsRecorder]]

## Eventos que publica
[[Eventos GameEvents#OnPlayerCharacterAssigned|OnPlayerCharacterAssigned]], [[Eventos GameEvents#OnPlayerJoined|OnPlayerJoined]], [[Eventos GameEvents#OnPlayerLeft|OnPlayerLeft]]

## Eventos que escucha
[[Eventos GameEvents#OnMatchPhaseChanged|OnMatchPhaseChanged]]
