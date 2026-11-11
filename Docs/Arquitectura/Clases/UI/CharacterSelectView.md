---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/CharacterSelectView.cs"
tags: [clase, modulo/ui]
---
# CharacterSelectView

> Character select panel: forwards per-character and per-player state to its cards and slots. Driven by the menu flow after each selection change. Lives on an always-active object and toggles `_panelRoot`.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/CharacterSelectView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterSelectView {
    <<MonoBehaviour>>
    -GameObject _panelRoot
    -CharacterCardView[] _cards
    -PlayerSlotStatusView[] _slots
    +GameObject MenuRoot
    +Show() void
    +Hide() void
    +RenderCard(CharacterDefinition character, bool hoverP1, bool hoverP2, PlayerId lockedBy) void
    +RenderSlot(PlayerId player, PlayerSlotState state, CharacterDefinition character) void
  }
  class CharacterCardView:::external
  <<MonoBehaviour>> CharacterCardView
  class PlayerSlotStatusView:::external
  <<MonoBehaviour>> PlayerSlotStatusView
  CharacterSelectView --> CharacterCardView
  CharacterSelectView --> PlayerSlotStatusView
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterCardView]], [[CharacterDefinition]], [[PlayerId]], [[PlayerSlotState]], [[PlayerSlotStatusView]]

## Usado por
[[MenuFlowController]]
