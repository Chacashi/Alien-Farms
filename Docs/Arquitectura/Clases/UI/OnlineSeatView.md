---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/OnlineSeatView.cs"
tags: [clase, modulo/ui]
---
# OnlineSeatView

> One player seat of the online room: reuses `PlayerSlotStatusView` for the status ("waiting for a player" / "choosing" / "ready") and adds the player's name and the host / you badges. Rendered on lobby changes.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/OnlineSeatView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineSeatView {
    <<MonoBehaviour>>
    -PlayerId _player
    -LocalizedString _player1Label
    -LocalizedString _player2Label
    -PlayerSlotStatusView _status
    -TMP_Text _name
    -GameObject _hostBadge
    -GameObject _youBadge
    +PlayerId Player
    +Render(OnlineLobbySeat seat, bool occupied, CharacterDefinition character) void
  }
  class LocalizedStringBinding:::external
  class PlayerSlotStatusView:::external
  <<MonoBehaviour>> PlayerSlotStatusView
  OnlineSeatView --> LocalizedStringBinding
  OnlineSeatView --> PlayerSlotStatusView
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[LocalizationKeys]], [[LocalizedStringBinding]], [[OnlineLobbySeat]], [[PlayerId]], [[PlayerSlotState]], [[PlayerSlotStatusView]]

## Usado por
[[OnlinePanelView]]
