---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: static
archivo: "Assets/Scripts/Networking/Session/SeatGraceRules.cs"
tags: [clase, modulo/networking]
---
# SeatGraceRules

> Pure rules of the reconnection grace on the server: what a disconnection does to a seat, which reservations expired and how much grace is left. Times are Time.realtimeSinceStartupAsDouble seconds.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/SeatGraceRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SeatGraceRules {
    <<static>>
    +OnDisconnect(bool noticedLeaving, bool graceEnabled)$ SeatDropOutcome
    +OnDisconnect(bool noticedLeaving, bool graceEnabled, bool serverStopping)$ SeatDropOutcome
    +ShouldRemoveRejectedMember(bool roomFull, bool serverStopping, bool hasUgsId, bool ugsIdInUse)$ bool
    +GraceLeft(PlayerSlot seat, double now, float graceSeconds)$ float
    +CollectExpired(ReadOnlySpan~PlayerSlot~ seats, double now, float graceSeconds, Span~PlayerId~ output)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ApprovalRules]], [[PlayerId]], [[PlayerSlot]], [[SeatDropOutcome]]

## Usado por
[[ConnectionApprovalHandler]]
