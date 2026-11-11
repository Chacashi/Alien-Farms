---
modulo: Data
namespace: AlienFarms.Data
tipo: class
archivo: "Assets/Scripts/Data/SessionData.cs"
tags: [clase, modulo/data]
---
# SessionData

> Plain C# store of the players' pre-match choices, one `SessionSlot` per `PlayerId`.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/SessionData.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SessionData {
    +Set(SessionSlot slot) bool
    +TryGet(PlayerId id, SessionSlot slot) bool
    +IsComplete(int requiredPlayers) bool
    +Clear() void
  }
  class SessionSlot:::external
  <<struct>> SessionSlot
  SessionData --> SessionSlot
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]], [[SessionSlot]]

## Usado por
[[MatchSession]]
