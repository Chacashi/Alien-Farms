---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: struct
archivo: "Assets/Scripts/Networking/Session/PlayerSlot.cs"
tags: [clase, modulo/networking]
---
# PlayerSlot

> Server-side record of one player seat. Reconnection fields (`UgsPlayerId`, `DisconnectedAt`, `SavedPosition`, `SavedFacingYaw`) are reserved for week 2 and not used yet.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/PlayerSlot.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerSlot {
    <<struct>>
    +PlayerId Id
    +ulong ClientId
    +FixedString32Bytes DisplayName
    +byte CharacterIndex
    +bool Connected
    +FixedString64Bytes UgsPlayerId
    +double DisconnectedAt
    +Vector3 SavedPosition
    +float SavedFacingYaw
    +bool IsOccupied
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]]

## Usado por
[[ConnectionApprovalHandler]], [[NetworkPlayerRoster]], [[ServerSlotDirectory]]
