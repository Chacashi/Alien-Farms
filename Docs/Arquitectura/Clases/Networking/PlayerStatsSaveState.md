---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: enumeration
archivo: "Assets/Scripts/Networking/Persistence/PlayerStatsSaveState.cs"
tags: [clase, modulo/networking]
---
# PlayerStatsSaveState

> Save state of the `PlayerStatsLedger` (mirrors PlayerProgressSaveStatus).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PlayerStatsSaveState.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerStatsSaveState {
    <<enumeration>>
    Idle
    Saving
    Saved
    Failed
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Idle`, `Saving`, `Saved`, `Failed`

## Usado por
[[PlayerStatsLedger]], [[PlayerStatsSync]]
