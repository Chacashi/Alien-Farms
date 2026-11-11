---
modulo: Core
namespace: AlienFarms.Core
tipo: MonoBehaviour
archivo: "Assets/Scripts/Core/PlayerIdentity.cs"
tags: [clase, modulo/core]
---
# PlayerIdentity

> Holds which local player owns this GameObject hierarchy.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/PlayerIdentity.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerIdentity {
    <<MonoBehaviour>>
    -PlayerId _id
    +PlayerId Id
    +Initialize(PlayerId id) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]]

## Usado por
[[Bomb]], [[MenuLobby]], [[NetworkPlayer]], [[PlayerGrabber]], [[PlayerMenuInput]], [[PlayerPauseInput]], [[PlayerRig]], [[PlayerStunReceiver]], [[PlayerTackle]]
