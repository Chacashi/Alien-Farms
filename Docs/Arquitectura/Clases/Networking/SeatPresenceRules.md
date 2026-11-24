---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: static
archivo: "Assets/Scripts/Networking/Match/SeatPresenceRules.cs"
tags: [clase, modulo/networking]
---
# SeatPresenceRules

> Pure rules of `NetworkSeatPresence`: which `PresenceChange`s a board transition publishes. No allocation.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/SeatPresenceRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SeatPresenceRules {
    <<static>>
    +Diff(SeatPresenceBoard previous, SeatPresenceBoard current, double now, Span~PresenceChange~ output)$ int
    +Current(SeatPresenceBoard board, double now, Span~PresenceChange~ output)$ int
    +GraceLeft(SeatPresence seat, double now)$ float
    +Publishes(SeatPresence previous, SeatPresence current)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerConnectionStatus]], [[PlayerId]], [[PresenceChange]], [[SeatPresence]], [[SeatPresenceBoard]], [[Status]]

## Usado por
[[NetworkSeatPresence]]
