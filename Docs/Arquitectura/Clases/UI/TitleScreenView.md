---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/TitleScreenView.cs"
tags: [clase, modulo/ui]
---
# TitleScreenView

> Applies title group visibility and input settings from the intro director.

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
    -CanvasGroup _attractGroup
    -CanvasGroup _promptGroup
    -CanvasGroup _menuGroup
    -GameObject _firstSelected
    +GameObject MenuRoot
    +GameObject FirstSelected
    +Show() void
    +Hide() void
    +SetAttractAlpha(float alpha) void
    +SetPromptAlpha(float alpha) void
    +SetMenuAlpha(float alpha) void
    +SetMenuInteractable(bool interactable) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[MenuFlowController]], [[TitleIntroDirector]]
