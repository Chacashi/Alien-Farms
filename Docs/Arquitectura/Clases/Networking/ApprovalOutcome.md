---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: enumeration
archivo: "Assets/Scripts/Networking/Session/ApprovalOutcome.cs"
tags: [clase, modulo/networking]
---
# ApprovalOutcome

> What an `ApprovalDecision` lets the connecting client do.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ApprovalOutcome.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ApprovalOutcome {
    <<enumeration>>
    Rejected
    AssignSeat
    Reconnect
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Rejected`, `AssignSeat`, `Reconnect`

## Usado por
[[ApprovalDecision]], [[ConnectionApprovalHandler]], [[ServerSlotDirectory]]
