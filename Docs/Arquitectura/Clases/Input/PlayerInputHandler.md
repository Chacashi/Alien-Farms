---
modulo: Input
namespace: AlienFarms.Input
tipo: MonoBehaviour
archivo: "Assets/Scripts/Input/PlayerInputHandler.cs"
tags: [clase, modulo/input]
---
# PlayerInputHandler

> Reads gameplay input from this player's own `PlayerInput`. The serialized `InputActionReference`s only identify actions; the live actions are resolved by id on `PlayerInput.actions`, which is a per-player clone under PlayerInputManager. It is the local `IPlayerCommandSource` of the player's gameplay components.

**Módulo:** [[Modulo Input|Input]] · **Namespace:** `AlienFarms.Input` · **Archivo:** `Assets/Scripts/Input/PlayerInputHandler.cs`

## Hereda / implementa
`MonoBehaviour`, [[IPlayerCommandSource]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerInputHandler {
    <<MonoBehaviour>>
    -PlayerInput _playerInput
    -InputActionReference _move
    -InputActionReference _look
    -InputActionReference _sprint
    -InputActionReference _grab
    -InputActionReference _pause
    -InputActionReference _aim
    -bool _lockCursorForMouse
    +event Action GrabStarted
    +event Action GrabCanceled
    +Vector2 Move
    +Vector2 Look
    +bool IsSprintHeld
    ...5 más
  }
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  PlayerInputHandler ..|> IPlayerCommandSource
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IPlayerCommandSource]], [[InputSchemeNames]]

## Usado por
[[Mode]], [[NetworkCarry]], [[NetworkPlayerInput]], [[PlayerMenuInput]], [[PlayerPauseInput]]
