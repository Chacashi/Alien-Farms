---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/PauseMenuView.cs"
tags: [clase, modulo/ui]
---
# PauseMenuView

> Shows the pause panel while the game is paused, with a subtitle naming who paused. Lives on an always-active object and toggles `_panelRoot`. Buttons are wired to PauseController in the scene.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/PauseMenuView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PauseMenuView {
    <<MonoBehaviour>>
    -LocalizedString _subtitleEntry
    -LocalizedString _player1Label
    -LocalizedString _player2Label
    -GameObject _panelRoot
    -TMP_Text _subtitle
  }
  class LocalizedStringBinding:::external
  class PlayerCharacterCache:::external
  PauseMenuView --> LocalizedStringBinding
  PauseMenuView --> PlayerCharacterCache
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[GameEvents]], [[LocalizationKeys]], [[LocalizedStringBinding]], [[PlayerCharacterCache]], [[PlayerId]]

## Eventos que escucha
[[Eventos GameEvents#OnGamePaused|OnGamePaused]], [[Eventos GameEvents#OnGameResumed|OnGameResumed]], [[Eventos GameEvents#OnPlayerCharacterAssigned|OnPlayerCharacterAssigned]]
