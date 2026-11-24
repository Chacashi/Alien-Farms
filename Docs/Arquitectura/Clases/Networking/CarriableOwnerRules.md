---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: static
archivo: "Assets/Scripts/Networking/Player/CarriableOwnerRules.cs"
tags: [clase, modulo/networking]
---
# CarriableOwnerRules

> Pure mapping from NGO ownership to what the tint / debug label / log show. On a host the server's client id is also P1's: what the host carries stays server-owned, so "owned by the server" means P1 only while P1 claims it.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/CarriableOwnerRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CarriableOwnerRules {
    <<static>>
    +Resolve(ulong ownerClientId, ulong serverClientId, PlayerId claimant, PlayerId ownerClientSeat)$ CarriableOwner
    +IsLabelShown(CarriableOwner owner, PlayerId claimant, bool isPureClient)$ bool
    +PreviousOwnerSeat(PlayerId lookedUp, PlayerId cached, PlayerId claimant)$ PlayerId
    +TagIndex(CarriableOwner owner)$ int
    +Tag(CarriableOwner owner)$ string
    +SeatTag(PlayerId seat)$ string
    +DescribeClient(ulong clientId, ulong serverClientId, PlayerId seat)$ string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CarriableOwner]], [[CarriableOwnerKind]], [[PlayerId]]

## Usado por
[[CarriableOwnershipLabel]], [[NameTagRules]], [[NetworkCarriable]], [[OwnershipLabelCache]]
