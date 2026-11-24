---
tags: [modulo, modulo/input]
---
# Módulo Input

Carpeta `Assets/Scripts/Input/` · 8 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class AxisStepDetector {
    +Update(float axis, float threshold) int
    +Reset() void
  }
  class DeferredDisableGate {
    +bool IsDisablePending
    +Request(bool active, bool isCurrentlyEnabled) bool
    +ConsumePendingDisable() bool
  }
  class HoldToPauseTracker {
    +float Duration
    +float Progress
    +Tick(bool held, float unscaledDeltaTime) bool
    +Reset() void
  }
  class InputMapNames {
    <<static>>
  }
  class InputSchemeNames {
    <<static>>
  }
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
    ...7 más
  }
  class PlayerPauseInput {
    <<MonoBehaviour>>
    -PauseSettings _settings
    -PlayerInputHandler _input
    -PlayerIdentity _identity
  }
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  class MenuSettings:::external
  <<ScriptableObject>> MenuSettings
  class PauseSettings:::external
  <<ScriptableObject>> PauseSettings
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  PlayerInputHandler ..|> IPlayerCommandSource
  PlayerMenuInput --> AxisStepDetector
  PlayerMenuInput --> DeferredDisableGate
  PlayerMenuInput --> MenuSettings
  PlayerMenuInput --> PlayerIdentity
  PlayerMenuInput --> PlayerInputHandler
  PlayerPauseInput --> HoldToPauseTracker
  PlayerPauseInput --> PauseSettings
  PlayerPauseInput --> PlayerIdentity
  PlayerPauseInput --> PlayerInputHandler
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[AxisStepDetector]] | class | Plain C# edge detector that turns a continuous axis (stick, dpad) into discrete steps: -1 or +1 once when the axis crosses the threshold, 0 … |
| [[DeferredDisableGate]] | class | Plain C# helper for a component that may be enabled at once but must only be disabled later (e.g. an EventSystem that cannot be disabled fro… |
| [[HoldToPauseTracker]] | class | Plain C# hold-to-confirm timer: accumulates while a button is held and reports completion once. After completing, the button must be release… |
| [[InputMapNames]] | static | Action map names defined in AlienFarmsControls.inputactions. |
| [[InputSchemeNames]] | static | Control scheme names defined in AlienFarmsControls.inputactions. |
| [[PlayerInputHandler]] | MonoBehaviour | Reads gameplay input from this player's own `PlayerInput`. The serialized `InputActionReference`s only identify actions; the live actions ar… |
| [[PlayerMenuInput]] | MonoBehaviour | Per-player menu navigation. `EnterMenu` switches this player's `PlayerInput` to the UI map and scopes its own `MultiplayerEventSystem` to a … |
| [[PlayerPauseInput]] | MonoBehaviour | Hold-to-pause for one player: holding this player's Pause button for `PauseSettings.HoldDuration` (unscaled) raises `GameEvents.OnPauseReque… |

## Depende de
[[Modulo Core|Core]] (6), [[Modulo Data|Data]] (2)