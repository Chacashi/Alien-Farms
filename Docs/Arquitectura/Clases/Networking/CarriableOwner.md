---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: struct
archivo: "Assets/Scripts/Networking/Player/CarriableOwner.cs"
tags: [clase, modulo/networking]
---
# CarriableOwner

> Resolved owner of a carriable object: the server, a seat, or a client not resolved yet.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/CarriableOwner.cs`

## Hereda / implementa
`IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CarriableOwner {
    <<struct>>
    +CarriableOwner Server$
    +CarriableOwner Unresolved$
    +CarriableOwnerKind Kind
    +PlayerId Seat
    +ForSeat(PlayerId seat)$ CarriableOwner
    +Equals(CarriableOwner other) bool
    +Equals(object obj) bool
    +GetHashCode() int
    +bool operator$
    +ToString() string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CarriableOwnerKind]], [[PlayerId]]

## Usado por
[[CarriableOwnerRules]], [[CarriableOwnerTint]], [[CarriableOwnershipLabel]], [[NetworkCarriable]], [[OwnershipLabelCache]]
