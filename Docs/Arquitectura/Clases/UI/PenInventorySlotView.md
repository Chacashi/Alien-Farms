---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/PenInventorySlotView.cs"
tags: [clase, modulo/ui]
---
# PenInventorySlotView

> One animal type in a pen inventory HUD: icon plus count. Driven by `PenInventoryView`.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/PenInventorySlotView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PenInventorySlotView {
    <<MonoBehaviour>>
    -Image _icon
    -TMP_Text _count
    +int Count
    +SetCount(int count) void
    +SetIconIfEmpty(Sprite sprite) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PenInventoryView]]
