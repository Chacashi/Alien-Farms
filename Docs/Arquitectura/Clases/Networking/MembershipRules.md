---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: static
archivo: "Assets/Scripts/Networking/Session/MembershipRules.cs"
tags: [clase, modulo/networking]
---
# MembershipRules

> Pure rules of reclaiming a lobby membership this player still holds (a join by code refused as 'already in a session'): only the session whose code is the one asked for is reconnected to, and only a session known to be another one is left (leaving one's own session would release the seat as Left).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/MembershipRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MembershipRules {
    <<static>>
    +Classify(string sessionCode, string joinCode)$ MembershipMatch
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MembershipMatch]], [[SessionCodeFormat]]

## Usado por
[[RelaySessionConnection]]
