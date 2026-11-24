---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: class
archivo: "Assets/Scripts/Networking/Session/ServerSlotDirectory.cs"
tags: [clase, modulo/networking]
---
# ServerSlotDirectory

> Server-side seat table: which client holds Player1 / Player2. Plain C#, owned by the approval handler and shared with the roster and the room through the runtime anchor. A seat is freed with `Release`, or kept for its player with `MarkDisconnected` until it reconnects (`ApprovalOutcome.Reconnect`) or `ReleaseExpired` drops it (grace, from 2.7).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ServerSlotDirectory.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ServerSlotDirectory {
    +ReadOnlySpan~PlayerSlot~ Seats
    +int ConnectedCount
    +int CountAwaiting
    +TryApply(ApprovalDecision decision, ulong clientId, ConnectionPayload payload) bool
    +TryAssign(PlayerId id, ulong clientId, ConnectionPayload payload) bool
    +TryReconnect(PlayerId id, ulong clientId, ConnectionPayload payload) bool
    +TrySetCharacter(PlayerId id, byte characterIndex) bool
    +TrySavePose(PlayerId id, Vector3 position, float facingYaw) bool
    +ClearSavedPose(PlayerId id) bool
    +TryFindAwaiting(FixedString64Bytes ugsPlayerId, PlayerId id) bool
    +IsUgsIdInUse(FixedString64Bytes ugsPlayerId) bool
    +TryGetByClient(ulong clientId, PlayerSlot slot) bool
    +TryGet(PlayerId id, PlayerSlot slot) bool
    ...5 más
  }
  class PlayerSlot:::external
  <<struct>> PlayerSlot
  ServerSlotDirectory --> PlayerSlot
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ApprovalDecision]], [[ApprovalOutcome]], [[ApprovalRules]], [[ConnectionPayload]], [[PlayerId]], [[PlayerSlot]]

## Usado por
[[ApprovalPolicy]], [[ConnectionApprovalHandler]], [[NetworkLobbyState]], [[NetworkPlayerRoster]], [[NetworkRuntimeAnchor]], [[ReconnectGraceCoordinator]]
