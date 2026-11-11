---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: struct
archivo: "Assets/Scripts/Networking/Session/ApprovalDecision.cs"
tags: [clase, modulo/networking]
---
# ApprovalDecision

> Outcome of `ApprovalRules.Evaluate`: approved, or rejected with a reason key sent to the client.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ApprovalDecision.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ApprovalDecision {
    <<struct>>
    +bool Approved
    +string Reason
    +Approve()$ ApprovalDecision
    +Reject(string reason)$ ApprovalDecision
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ApprovalRules]], [[ConnectionApprovalHandler]]
