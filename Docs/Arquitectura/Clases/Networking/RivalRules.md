---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: static
archivo: "Assets/Scripts/Networking/Persistence/RivalRules.cs"
tags: [clase, modulo/networking]
---
# RivalRules

> Whose public stats the online room shows as the rival's. No allocation.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/RivalRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class RivalRules {
    <<static>>
    +TryFindRival(ReadOnlySpan~OnlineParticipant~ participants, string ugsPlayerId)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[OnlineParticipant]]

## Usado por
[[PlayerStatsSync]]
