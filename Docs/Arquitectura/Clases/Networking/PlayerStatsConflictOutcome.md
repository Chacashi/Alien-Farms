---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: enumeration
archivo: "Assets/Scripts/Networking/Persistence/PlayerStatsConflictOutcome.cs"
tags: [clase, modulo/networking]
---
# PlayerStatsConflictOutcome

> What `PlayerStatsLedger.ResolveConflict` concluded from the reloaded record.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PlayerStatsConflictOutcome.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerStatsConflictOutcome {
    <<enumeration>>
    Ignored
    AlreadyApplied
    Rebased
    ReadOnly
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Ignored`, `AlreadyApplied`, `Rebased`, `ReadOnly`

## Usado por
[[PlayerStatsLedger]], [[PlayerStatsSync]]
