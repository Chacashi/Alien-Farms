---
modulo: Input
namespace: AlienFarms.Input
tipo: MonoBehaviour
archivo: "Assets/Scripts/Input/PlayerMenuInput.cs"
tags: [clase, modulo/input]
---
# PlayerMenuInput

> Per-player menu navigation. `EnterMenu` switches this player's `PlayerInput` to the UI map and scopes its own `MultiplayerEventSystem` to a menu root; `ExitMenu` restores gameplay. `SetBlocked` deactivates all of this player's input (used for the player who does not own a menu). Also exposes discrete navigation steps and Submit/Cancel for menus driven by code instead of Selectables.

**Módulo:** [[Modulo Input|Input]] · **Namespace:** `AlienFarms.Input` · **Archivo:** `Assets/Scripts/Input/PlayerMenuInput.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerMenuInput {
    <<MonoBehaviour>>
    -MenuSettings _settings
    -PlayerInput _playerInput
    -PlayerIdentity _identity
    -MultiplayerEventSystem _eventSystem
    -PlayerInputHandler _gameplayInput
    -InputActionReference _navigate
    -InputActionReference _submit
    -InputActionReference _cancel
    -AxisStepDetector _horizontal
    +event Action~int~ HorizontalStep
    +event Action~int~ VerticalStep
    +event Action Submitted
    +event Action Canceled
    ...6 más
  }
  class AxisStepDetector:::external
  class DeferredDisableGate:::external
  class MenuSettings:::external
  <<ScriptableObject>> MenuSettings
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  class PlayerInputHandler:::external
  <<MonoBehaviour>> PlayerInputHandler
  PlayerMenuInput --> AxisStepDetector
  PlayerMenuInput --> DeferredDisableGate
  PlayerMenuInput --> MenuSettings
  PlayerMenuInput --> PlayerIdentity
  PlayerMenuInput --> PlayerInputHandler
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AxisStepDetector]], [[DeferredDisableGate]], [[GameEvents]], [[InputMapNames]], [[MenuSettings]], [[PlayerId]], [[PlayerIdentity]], [[PlayerInputHandler]]

## Usado por
[[MenuFlowController]], [[MenuLobby]], [[PlayerMenuRouter]], [[PlayerRig]]

## Eventos que publica
[[Eventos GameEvents#OnMenuCancelPressed|OnMenuCancelPressed]], [[Eventos GameEvents#OnPlayerDeviceLost|OnPlayerDeviceLost]], [[Eventos GameEvents#OnPlayerDeviceRegained|OnPlayerDeviceRegained]]
