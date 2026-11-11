---
modulo: UI
namespace: AlienFarms.UI
tipo: class
archivo: "Assets/Scripts/UI/LocalizedStringBinding.cs"
tags: [clase, modulo/ui]
---
# LocalizedStringBinding

> Keeps the resolved value of a `LocalizedString` and notifies when it changes (load or locale change). `Value` stays null until the string resolves: callers must then leave the scene text untouched. Call `Enable` from OnEnable and `Disable` from OnDisable.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/LocalizedStringBinding.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LocalizedStringBinding {
    +string Value
    +Enable() void
    +Disable() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[CharacterCardView]], [[CountdownView]], [[MatchResultsView]], [[PauseMenuView]], [[PlayerSlotStatusView]]
