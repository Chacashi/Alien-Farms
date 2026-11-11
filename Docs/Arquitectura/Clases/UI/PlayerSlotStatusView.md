---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/PlayerSlotStatusView.cs"
tags: [clase, modulo/ui]
---
# PlayerSlotStatusView

> One player's status on the character select screen ("press to join" / "choosing" / "ready"), with the frame tinted by the hovered character's Theme.Primary. Text is only rewritten when the state changes.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/PlayerSlotStatusView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerSlotStatusView {
    <<MonoBehaviour>>
    -PlayerId _player
    -LocalizedString _emptyEntry
    -LocalizedString _choosingEntry
    -LocalizedString _readyEntry
    -Color _emptyFrameColor
    -TMP_Text _status
    -Image _frame
    +PlayerId Player
    +Render(PlayerSlotState state, CharacterDefinition character) void
  }
  class LocalizedStringBinding:::external
  PlayerSlotStatusView --> LocalizedStringBinding
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[LocalizationKeys]], [[LocalizedStringBinding]], [[PlayerId]], [[PlayerSlotState]]

## Usado por
[[CharacterSelectView]]
