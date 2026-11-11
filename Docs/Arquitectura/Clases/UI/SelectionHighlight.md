---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/SelectionHighlight.cs"
tags: [clase, modulo/ui]
---
# SelectionHighlight

> Focus feedback for a menu button: shows a focus frame and scales up while selected. Hovering with the mouse selects the button (if interactable), so mouse and gamepad/keyboard share one focus. Animates with unscaled time so it works while the game is paused.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/SelectionHighlight.cs`

## Hereda / implementa
`MonoBehaviour`, `ISelectHandler`, `IDeselectHandler`, `IPointerEnterHandler`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SelectionHighlight {
    <<MonoBehaviour>>
    -float _selectedScale
    -float _scaleSpeed
    -GameObject _focusFrame
    -RectTransform _target
    +OnSelect(BaseEventData eventData) void
    +OnDeselect(BaseEventData eventData) void
    +OnPointerEnter(PointerEventData eventData) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```
