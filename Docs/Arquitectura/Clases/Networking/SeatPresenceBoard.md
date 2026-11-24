---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: struct
archivo: "Assets/Scripts/Networking/Match/SeatPresenceBoard.cs"
tags: [clase, modulo/networking]
---
# SeatPresenceBoard

> Presence of both player seats, replicated by `NetworkSeatPresence`. Written whole by the server (rebuilt from the seat table), so it stays a small fixed-size value (36 bytes).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/SeatPresenceBoard.cs`

## Hereda / implementa
`INetworkSerializable`, `IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SeatPresenceBoard {
    <<struct>>
    +SeatPresence Player1
    +SeatPresence Player2
    +Get(PlayerId id) SeatPresence
    +With(PlayerId id, SeatPresence presence) SeatPresenceBoard
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(SeatPresenceBoard other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  class SeatPresence:::external
  <<struct>> SeatPresence
  SeatPresenceBoard --> SeatPresence
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]], [[SeatPresence]]

## Usado por
[[NetworkSeatPresence]], [[ReconnectGraceCoordinator]], [[ReconnectGraceRules]], [[SeatPresenceRules]]
