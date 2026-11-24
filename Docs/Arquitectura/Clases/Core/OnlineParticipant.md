---
modulo: Core
namespace: AlienFarms.Core
tipo: struct
archivo: "Assets/Scripts/Core/OnlineParticipant.cs"
tags: [clase, modulo/core]
---
# OnlineParticipant

> One member of the online session as listed by Sessions (host first, then by join order).

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/OnlineParticipant.cs`

## Hereda / implementa
`IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineParticipant {
    <<struct>>
    +string UgsPlayerId
    +string DisplayName
    +bool IsHost
    +bool IsLocal
    +Equals(OnlineParticipant other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[DevConnectionPanel]], [[OnlineMenuController]], [[OnlineSessionChannel]], [[OnlineSessionService]], [[PlayerStatsSync]], [[RivalRules]], [[SessionExit]], [[Status]]
