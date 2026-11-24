---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: enumeration
archivo: "Assets/Scripts/Networking/Session/MembershipMatch.cs"
tags: [clase, modulo/networking]
---
# MembershipMatch

> How a joined session (read through a probe) relates to the join code asked for (`MembershipRules.Classify`).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/MembershipMatch.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MembershipMatch {
    <<enumeration>>
    Unknown
    Same
    Other
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Unknown`, `Same`, `Other`

## Usado por
[[MembershipRules]], [[RelaySessionConnection]]
