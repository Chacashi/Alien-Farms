---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/PlayerProgressSaveStatus.cs"
tags: [clase, modulo/core]
---
# PlayerProgressSaveStatus

> State of the last write of the player's persistent progress.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/PlayerProgressSaveStatus.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerProgressSaveStatus {
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
[[PlayerProgressChannel]], [[PlayerStatsSync]], [[SaveStatusBadgeView]], [[TitleStatsView]]
