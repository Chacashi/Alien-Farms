---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: enumeration
archivo: "Assets/Scripts/Networking/Session/SeatDropOutcome.cs"
tags: [clase, modulo/networking]
---
# SeatDropOutcome

> What the server does with the seat of a client that disconnected (see `SeatGraceRules.OnDisconnect`).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/SeatDropOutcome.cs`

## Hereda / implementa
`byte`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SeatDropOutcome {
    <<enumeration>>
    Released
    Left
    Reserved
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Released`, `Left`, `Reserved`

## Usado por
[[ConnectionApprovalHandler]], [[SeatGraceRules]]
