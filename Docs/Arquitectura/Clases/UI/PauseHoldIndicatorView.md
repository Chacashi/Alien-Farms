---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/PauseHoldIndicatorView.cs"
tags: [clase, modulo/ui]
---
# PauseHoldIndicatorView

> Shows one player's hold-to-pause progress as a filled bar. Appears only after holding for `PauseSettings.ShowProgressAfter`, never while paused or in a phase that cannot pause. Lives on an always-active object and toggles `_root`.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/PauseHoldIndicatorView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PauseHoldIndicatorView {
    <<MonoBehaviour>>
    -PlayerId _player
    -PauseSettings _settings
    -GameObject _root
    -Image _fill
  }
  class PauseSettings:::external
  <<ScriptableObject>> PauseSettings
  PauseHoldIndicatorView --> PauseSettings
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[MatchPhase]], [[PauseRules]], [[PauseSettings]], [[PlayerId]]

## Eventos que escucha
[[Eventos GameEvents#OnGamePaused|OnGamePaused]], [[Eventos GameEvents#OnGameResumed|OnGameResumed]], [[Eventos GameEvents#OnMatchPhaseChanged|OnMatchPhaseChanged]], [[Eventos GameEvents#OnPauseHoldProgress|OnPauseHoldProgress]]
