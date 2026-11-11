---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/TitleScreenView.cs"
tags: [clase, modulo/ui]
---
# TitleScreenView

> Title screen panel: shows the "press to start" prompt until the first player joins, then the menu buttons. Lives on an always-active object and toggles `_panelRoot`. Buttons are wired in the scene.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/TitleScreenView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class TitleScreenView {
    <<MonoBehaviour>>
    -GameObject _panelRoot
    -GameObject _promptRoot
    -GameObject _buttonsRoot
    -GameObject _firstSelected
    +GameObject MenuRoot
    +GameObject FirstSelected
    +Show() void
    +Hide() void
    +SetPlayerJoined(bool joined) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[MenuFlowController]]
