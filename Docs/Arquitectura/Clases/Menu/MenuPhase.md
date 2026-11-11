---
modulo: Menu
namespace: AlienFarms.Menu
tipo: enumeration
archivo: "Assets/Scripts/Menu/MenuPhase.cs"
tags: [clase, modulo/menu]
---
# MenuPhase

> Screens of the MainMenu scene, in flow order.

**Módulo:** [[Modulo Menu|Menu]] · **Namespace:** `AlienFarms.Menu` · **Archivo:** `Assets/Scripts/Menu/MenuPhase.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MenuPhase {
    <<enumeration>>
    Title
    CharacterSelect
    Loading
    SubMenu
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Title`, `CharacterSelect`, `Loading`, `SubMenu`

## Usado por
[[MenuFlowController]]
