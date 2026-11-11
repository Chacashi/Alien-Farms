---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/LanguageSelector.cs"
tags: [clase, modulo/ui]
---
# LanguageSelector

> Options row that cycles the game language. Left/Right (stick, arrows) and Submit change the locale; Up/Down navigation stays with the Selectable (Explicit, left/right empty). Arrow buttons call `Previous` / `Next` from their onClick. The choice is global and persisted by the PlayerPrefs startup selector.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/LanguageSelector.cs`

## Hereda / implementa
`MonoBehaviour`, `IMoveHandler`, `ISubmitHandler`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LanguageSelector {
    <<MonoBehaviour>>
    -TMP_Text _valueLabel
    +Next() void
    +Previous() void
    +OnMove(AxisEventData eventData) void
    +OnSubmit(BaseEventData eventData) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LocaleCycle]]
