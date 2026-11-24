---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: struct
archivo: "Assets/Scripts/Networking/Match/SeatPresence.cs"
tags: [clase, modulo/networking]
---
# SeatPresence

> Replicated presence of one player seat: who holds it and its `PlayerConnectionStatus`. A seat held for a dropped player is `PlayerConnectionStatus.Reconnecting` until `GraceEndTime` (server time). A free seat keeps how it was last released (Left / TimedOut), or Connected when it was never released. Fixed 18 bytes on the wire.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/SeatPresence.cs`

## Hereda / implementa
`INetworkSerializable`, `IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SeatPresence {
    <<struct>>
    +SeatPresence Empty$
    +bool IsSeated
    +ulong ClientId
    +PlayerConnectionStatus Status
    +double GraceEndTime
    +Connected(ulong clientId)$ SeatPresence
    +Reconnecting(ulong clientId, double graceEndTime)$ SeatPresence
    +Released(PlayerConnectionStatus status)$ SeatPresence
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(SeatPresence other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerConnectionStatus]], [[Status]]

## Usado por
[[NetworkSeatPresence]], [[ReconnectGraceRules]], [[SeatPresenceBoard]], [[SeatPresenceRules]]
