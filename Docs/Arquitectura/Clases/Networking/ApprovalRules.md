---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: static
archivo: "Assets/Scripts/Networking/Session/ApprovalRules.cs"
tags: [clase, modulo/networking]
---
# ApprovalRules

> Pure connection approval rules, in order: same protocol, valid payload, one connection per UGS player (a dropped player gets its own seat back while the grace lasts), a free seat (Player1 first), otherwise "room full". Every rejection carries a `DisconnectReasons` key the client maps to localized text.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ApprovalRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ApprovalRules {
    <<static>>
    +CheckMembership(bool isHostClient, bool onlineSession, bool hasUgsPlayerId)$ SessionMembership
    +NeedsIdentity(bool payloadValid, ConnectionPayload payload, ApprovalPolicy policy)$ bool
    +Evaluate(bool payloadValid, ConnectionPayload payload, ApprovalPolicy policy)$ ApprovalDecision
    +IsSeatFree(PlayerSlot seat, double now, float graceSeconds)$ bool
    +IsReservationExpired(PlayerSlot seat, double now, float graceSeconds)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ApprovalDecision]], [[ApprovalPolicy]], [[ConnectionPayload]], [[DisconnectReasons]], [[PlayerId]], [[PlayerSlot]], [[SessionMembership]]

## Usado por
[[ConnectionApprovalHandler]], [[SeatGraceRules]], [[ServerSlotDirectory]]
