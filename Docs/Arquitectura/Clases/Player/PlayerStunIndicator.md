---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/PlayerStunIndicator.cs"
tags: [clase, modulo/player]
---
# PlayerStunIndicator

> Shows a spinning ring of stars over the player while it is stunned (blast or tackle).

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/PlayerStunIndicator.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerStunIndicator {
    <<MonoBehaviour>>
    -PlayerMovement _movement
    -Transform _indicator
    -float _spinSpeed
  }
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  PlayerStunIndicator --> PlayerMovement
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerMovement]]
