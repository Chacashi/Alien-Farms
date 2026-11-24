---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: enumeration
archivo: "Assets/Scripts/Networking/Session/SessionMembership.cs"
tags: [clase, modulo/networking]
---
# SessionMembership

> Whether the UGS player id a client declared in its `ConnectionPayload` belongs to the host's online session (`ApprovalRules.CheckMembership`).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/SessionMembership.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SessionMembership {
    <<enumeration>>
    NotRequired
    Member
    Unconfirmed
    Rejected
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`NotRequired`, `Member`, `Unconfirmed`, `Rejected`

## Usado por
[[ApprovalRules]], [[ConnectionApprovalHandler]]
