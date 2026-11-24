---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/OnlineScreen.cs"
tags: [clase, modulo/core]
---
# OnlineScreen

> Screens of the online menu panel (resolved by AlienFarms.Menu.OnlineMenuRules.Resolve).

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/OnlineScreen.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineScreen {
    <<enumeration>>
    Hub
    Join
    Room
    Busy
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Hub`, `Join`, `Room`, `Busy`

## Usado por
[[OnlineMenuController]], [[OnlineMenuRules]], [[OnlinePanelView]]
