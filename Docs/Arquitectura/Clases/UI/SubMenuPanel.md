---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/SubMenuPanel.cs"
tags: [clase, modulo/ui]
---
# SubMenuPanel

> A sub-menu opened from a parent menu (Options, How to play...). Lives on the panel root itself: showing or hiding it toggles this GameObject. The parent controller scopes the owner's event system to `MenuRoot`.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/SubMenuPanel.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SubMenuPanel {
    <<MonoBehaviour>>
    -GameObject _firstSelected
    -GameObject _returnSelection
    +GameObject MenuRoot
    +GameObject FirstSelected
    +GameObject ReturnSelection
    +Show() void
    +Hide() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[MenuFlowController]], [[PauseController]]
