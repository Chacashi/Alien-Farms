---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/SaveStatusBadgeView.cs"
tags: [clase, modulo/ui]
---
# SaveStatusBadgeView

> Cloud save badge on the results panel: "Saving… / Progress saved / Could not save (will retry)". Visible only in online matches and while the `PlayerProgressChannel` reports a save status other than Idle. Redraws only when the channel changes (or a string resolves), never per frame.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/SaveStatusBadgeView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SaveStatusBadgeView {
    <<MonoBehaviour>>
    -PlayerProgressChannel _progress
    -MatchSession _session
    -LocalizedString _saving
    -LocalizedString _saved
    -LocalizedString _failed
    -GameObject _root
    -TMP_Text _label
  }
  class LocalizedStringBinding:::external
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  class PlayerProgressChannel:::external
  <<ScriptableObject>> PlayerProgressChannel
  SaveStatusBadgeView --> LocalizedStringBinding
  SaveStatusBadgeView --> MatchSession
  SaveStatusBadgeView --> PlayerProgressChannel
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameMode]], [[LocalizationKeys]], [[LocalizedStringBinding]], [[MatchSession]], [[Mode]], [[PlayerProgressChannel]], [[PlayerProgressSaveStatus]]
