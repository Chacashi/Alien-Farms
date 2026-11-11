---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/PlayerStunReceiver.cs"
tags: [clase, modulo/player]
---
# PlayerStunReceiver

> Blast target on the player root: drops whatever is carried (thrown away from the blast), knocks the player back and stuns it. Stuns never stack: the longest remaining one wins (`PlayerMovement.ApplyKnockback`). As a `SimulationRole.Replica` blasts are ignored (the authority applies them).

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/PlayerStunReceiver.cs`

## Hereda / implementa
`MonoBehaviour`, [[IStunnable]], [[ISimulationRoleTarget]]

## Diagrama
```mermaid
classDiagram
  direction LR
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
  class ISimulationRoleTarget:::external
  <<interface>> ISimulationRoleTarget
  class IStunnable:::external
  <<interface>> IStunnable
  class PlayerGrabber:::external
  <<MonoBehaviour>> PlayerGrabber
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  class PlayerSettings:::external
  <<ScriptableObject>> PlayerSettings
  PlayerStunReceiver --> PlayerGrabber
  PlayerStunReceiver --> PlayerIdentity
  PlayerStunReceiver --> PlayerMovement
  PlayerStunReceiver --> PlayerSettings
  PlayerStunReceiver ..|> ISimulationRoleTarget
  PlayerStunReceiver ..|> IStunnable
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[DropReason]], [[GameEvents]], [[ISimulationRoleTarget]], [[IStunnable]], [[PlanarMath]], [[PlayerGrabber]], [[PlayerId]], [[PlayerIdentity]], [[PlayerMovement]], [[PlayerSettings]], [[SimulationRole]]

## Usado por
[[PlayerRig]]

## Eventos que publica
[[Eventos GameEvents#OnPlayerStunned|OnPlayerStunned]]
