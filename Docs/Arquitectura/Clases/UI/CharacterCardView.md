---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/CharacterCardView.cs"
tags: [clase, modulo/ui]
---
# CharacterCardView

> One character card on the select screen: portrait and name of its character, which players hover it and who locked it. Rendered by `CharacterSelectView` after each selection change (not per frame).

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/CharacterCardView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterCardView {
    <<MonoBehaviour>>
    -CharacterDefinition _character
    -LocalizedString _takenByEntry
    -LocalizedString _player1Label
    -LocalizedString _player2Label
    -Image _portrait
    -TMP_Text _name
    -GameObject _lockedOverlay
    -TMP_Text _lockedBy
    -GameObject _cursorP1
    -GameObject _cursorP2
    -GameObject _readyStamp
    +CharacterDefinition Character
    +Render(bool hoverP1, bool hoverP2, PlayerId lockedBy) void
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  class LocalizedStringBinding:::external
  CharacterCardView --> CharacterDefinition
  CharacterCardView --> LocalizedStringBinding
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[LocalizationKeys]], [[LocalizedStringBinding]], [[PlayerId]]

## Usado por
[[CharacterSelectView]]
