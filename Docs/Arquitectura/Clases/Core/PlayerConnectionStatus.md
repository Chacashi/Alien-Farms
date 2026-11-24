---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/PlayerConnectionStatus.cs"
tags: [clase, modulo/core]
---
# PlayerConnectionStatus

> Online presence of a player seat (byte-sized: it is replicated in the seat presence board).

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/PlayerConnectionStatus.cs`

## Hereda / implementa
`byte`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerConnectionStatus {
    <<enumeration>>
    Connected
    Reconnecting
    Left
    TimedOut
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Connected`, `Reconnecting`, `Left`, `TimedOut`

## Usado por
[[ConnectionApprovalHandler]], [[ConnectionStatusRules]], [[ConnectionStatusView]], [[GameEvents]], [[MatchConnectionCueTracker]], [[MatchStatusSfxPlayer]], [[PresenceChange]], [[ReconnectGraceCoordinator]], [[ReconnectGraceRules]], [[ResultsMenuController]], [[SeatPresence]], [[SeatPresenceRules]]
