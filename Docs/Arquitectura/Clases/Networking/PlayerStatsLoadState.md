---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: enumeration
archivo: "Assets/Scripts/Networking/Persistence/PlayerStatsLoadState.cs"
tags: [clase, modulo/networking]
---
# PlayerStatsLoadState

> Load state of the `PlayerStatsLedger`.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PlayerStatsLoadState.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerStatsLoadState {
    <<enumeration>>
    NotLoaded
    Loading
    Loaded
    Failed
    ReadOnly
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`NotLoaded`, `Loading`, `Loaded`, `Failed`, `ReadOnly`

## Usado por
[[PlayerStatsLedger]], [[PlayerStatsSync]]
