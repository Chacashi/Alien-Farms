---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/PlayerTackle.cs"
tags: [clase, modulo/player]
---
# PlayerTackle

> Stealing by tackle: a sprinting player that runs into a carrier (fast enough and facing it) makes the carrier drop its animal, get knocked back and briefly stunned. Evaluated on the attacker's side of the collision.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/PlayerTackle.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
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
  class PlayerGrabber:::external
  <<MonoBehaviour>> PlayerGrabber
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  class PlayerSettings:::external
  <<ScriptableObject>> PlayerSettings
  PlayerTackle --> PlayerGrabber
  PlayerTackle --> PlayerIdentity
  PlayerTackle --> PlayerMovement
  PlayerTackle --> PlayerSettings
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[DropReason]], [[LayerNames]], [[PlayerGrabber]], [[PlayerId]], [[PlayerIdentity]], [[PlayerMovement]], [[PlayerSettings]]

## Usado por
[[PlayerRig]]
