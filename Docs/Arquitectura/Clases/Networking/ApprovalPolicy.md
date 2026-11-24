---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: struct
archivo: "Assets/Scripts/Networking/Session/ApprovalPolicy.cs"
tags: [clase, modulo/networking]
---
# ApprovalPolicy

> Session limits the `ApprovalRules` apply. Immutable; built from `NetworkSettings`.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ApprovalPolicy.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ApprovalPolicy {
    <<struct>>
    +int ProtocolVersion
    +int PlayerSeats
    +float ReconnectGraceSeconds
    +From(NetworkSettings settings)$ ApprovalPolicy
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[NetworkSettings]], [[ServerSlotDirectory]]

## Usado por
[[ApprovalRules]], [[ConnectionApprovalHandler]]
