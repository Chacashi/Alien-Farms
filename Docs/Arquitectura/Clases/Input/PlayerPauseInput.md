---
modulo: Input
namespace: AlienFarms.Input
tipo: MonoBehaviour
archivo: "Assets/Scripts/Input/PlayerPauseInput.cs"
tags: [clase, modulo/input]
---
# PlayerPauseInput

> Hold-to-pause for one player: holding this player's Pause button for `PauseSettings.HoldDuration` (unscaled) raises `GameEvents.OnPauseRequested`. Progress is published only while it changes.

**Módulo:** [[Modulo Input|Input]] · **Namespace:** `AlienFarms.Input` · **Archivo:** `Assets/Scripts/Input/PlayerPauseInput.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerPauseInput {
    <<MonoBehaviour>>
    -PauseSettings _settings
    -PlayerInputHandler _input
    -PlayerIdentity _identity
  }
  class HoldToPauseTracker:::external
  class PauseSettings:::external
  <<ScriptableObject>> PauseSettings
  class PlayerIdentity:::external
  <<MonoBehaviour>> PlayerIdentity
  class PlayerInputHandler:::external
  <<MonoBehaviour>> PlayerInputHandler
  PlayerPauseInput --> HoldToPauseTracker
  PlayerPauseInput --> PauseSettings
  PlayerPauseInput --> PlayerIdentity
  PlayerPauseInput --> PlayerInputHandler
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[HoldToPauseTracker]], [[PauseSettings]], [[PlayerIdentity]], [[PlayerInputHandler]]

## Eventos que publica
[[Eventos GameEvents#OnPauseHoldProgress|OnPauseHoldProgress]], [[Eventos GameEvents#OnPauseRequested|OnPauseRequested]]
