---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: static
archivo: "Assets/Scripts/Networking/Match/ReconnectGraceRules.cs"
tags: [clase, modulo/networking]
---
# ReconnectGraceRules

> Pure rules of the `ReconnectGraceCoordinator`: the seat table (realtime clock) turned into the replicated `SeatPresenceBoard` (server clock). No allocation.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/ReconnectGraceRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ReconnectGraceRules {
    <<static>>
    +GraceEndServerTime(double serverNow, double realtimeNow, double disconnectedAt, float grace)$ double
    +AnyAwaiting(ReadOnlySpan~PlayerSlot~ seats)$ bool
    +BuildSeat(PlayerSlot seat, PlayerConnectionStatus lastRelease, double serverNow, double realtimeNow, float grace)$ SeatPresence
    +KeepReservation(SeatPresence previous, SeatPresence rebuilt)$ SeatPresence
    +BuildBoard()$ SeatPresenceBoard
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerConnectionStatus]], [[PlayerSlot]], [[SeatPresence]], [[SeatPresenceBoard]], [[Status]]

## Usado por
[[ReconnectGraceCoordinator]]
