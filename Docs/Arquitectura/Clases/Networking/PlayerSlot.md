---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: struct
archivo: "Assets/Scripts/Networking/Session/PlayerSlot.cs"
tags: [clase, modulo/networking]
---
# PlayerSlot

> Server-side record of one player seat (plan: PlayerId, UgsPlayerId, ClientId, Name, Character, Connected, DisconnectedAt, SavedPose). A seat whose client dropped keeps `Connected` false and `DisconnectedAt` while it waits for the same `UgsPlayerId` to reconnect (`ServerSlotDirectory.MarkDisconnected`, used from 2.7).

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
    +bool HasSavedPose
    +Vector3 SavedPosition
    +float SavedFacingYaw
    +bool IsOccupied
    +bool HasCharacter
    +bool IsAwaitingReconnect
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]]

## Usado por
[[ApprovalRules]], [[ConnectionApprovalHandler]], [[LobbyRules]], [[LobbySlotEntry]], [[NetworkLobbyState]], [[NetworkPlayer]], [[NetworkPlayerRoster]], [[ReconnectGraceCoordinator]], [[ReconnectGraceRules]], [[SeatGraceRules]], [[ServerSlotDirectory]]
