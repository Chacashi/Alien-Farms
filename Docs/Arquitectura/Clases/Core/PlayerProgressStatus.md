---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/PlayerProgressStatus.cs"
tags: [clase, modulo/core]
---
# PlayerProgressStatus

> Load state of the player's persistent progress (cloud stats).

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/PlayerProgressStatus.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerProgressStatus {
    <<enumeration>>
    Unavailable
    Loading
    Loaded
    Failed
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Unavailable`, `Loading`, `Loaded`, `Failed`

## Usado por
[[PlayerProgressChannel]]
