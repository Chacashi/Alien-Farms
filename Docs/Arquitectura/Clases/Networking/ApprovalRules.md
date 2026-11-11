---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: static
archivo: "Assets/Scripts/Networking/Session/ApprovalRules.cs"
tags: [clase, modulo/networking]
---
# ApprovalRules

> Pure connection approval rules (version 1.5): valid payload, same protocol and a free seat. Reconnection to a previous seat and spectators come in week 2. Reasons are stable keys the client maps to localized text.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ApprovalRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ApprovalRules {
    <<static>>
    +Evaluate(bool payloadValid, ConnectionPayload payload, int protocolVersion, int occupiedSeats, int maxPlayers)$ ApprovalDecision
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ApprovalDecision]], [[ConnectionPayload]], [[DisconnectReasons]]

## Usado por
[[ConnectionApprovalHandler]]
