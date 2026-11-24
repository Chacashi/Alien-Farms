---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/OnlinePanelView.cs"
tags: [clase, modulo/ui]
---
# OnlinePanelView

> Online panel (hub, join, room, busy) of the MainMenu. Pure view: everything lives in the scene and is referenced here; `OnlineMenuController` drives it on channel changes (never per frame). Lives on an always-active object and toggles `_panelRoot`. Dynamic texts (error, busy label, hint) swap the entry of a `LocalizedString`; their text arrives through StringChanged, so strings are only built on changes.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/OnlinePanelView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlinePanelView {
    <<MonoBehaviour>>
    -LocalizedString _errorEntry
    -LocalizedString _busyEntry
    -LocalizedString _signedInEntry
    -LocalizedString _rejoinEntry
    -LocalizedString _hintEntry
    -GameObject _panelRoot
    -GameObject _hubRoot
    -GameObject _joinRoot
    -GameObject _roomRoot
    -GameObject _busyRoot
    -GameObject _errorRoot
    -GameObject _hubFirst
    -GameObject _joinFirst
    ...47 más
  }
  class CharacterCardButton:::external
  <<MonoBehaviour>> CharacterCardButton
  class CharacterCardView:::external
  <<MonoBehaviour>> CharacterCardView
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  class LocalizedStringBinding:::external
  class OnlineSeatView:::external
  <<MonoBehaviour>> OnlineSeatView
  OnlinePanelView --> CharacterCardButton
  OnlinePanelView --> CharacterCardView
  OnlinePanelView --> CharacterDefinition
  OnlinePanelView --> LocalizedStringBinding
  OnlinePanelView --> OnlineSeatView
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterCardButton]], [[CharacterCardView]], [[CharacterDefinition]], [[DisplayNameSanitizer]], [[LocalizationKeys]], [[LocalizedStringBinding]], [[LocalizedText]], [[Mode]], [[OnlineLobbyChannel]], [[OnlineLobbySeat]], [[OnlineScreen]], [[OnlineSeatView]], [[PlayerId]]

## Usado por
[[OnlineMenuController]]
