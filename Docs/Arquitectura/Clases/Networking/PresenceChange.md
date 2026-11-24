---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: struct
archivo: "Assets/Scripts/Networking/Match/PresenceChange.cs"
tags: [clase, modulo/networking]
---
# PresenceChange

> One `GameEvents.OnPlayerConnectionChanged` to raise, as found by `SeatPresenceRules`.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/PresenceChange.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PresenceChange {
    <<struct>>
    +PlayerId Id
    +PlayerConnectionStatus Status
    +float GraceLeft
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerConnectionStatus]], [[PlayerId]], [[Status]]

## Usado por
[[NetworkSeatPresence]], [[SeatPresenceRules]]
