---
modulo: UI
namespace: AlienFarms.UI
tipo: enumeration
archivo: "Assets/Scripts/UI/PlayerSlotState.cs"
tags: [clase, modulo/ui]
---
# PlayerSlotState

> Status of one player's slot on the character select screen.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/PlayerSlotState.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerSlotState {
    <<enumeration>>
    Empty
    Choosing
    Ready
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Empty`, `Choosing`, `Ready`

## Usado por
[[CharacterSelectView]], [[MenuFlowController]], [[OnlineSeatView]], [[PlayerSlotStatusView]]
