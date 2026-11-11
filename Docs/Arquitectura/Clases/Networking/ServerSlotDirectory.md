---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: class
archivo: "Assets/Scripts/Networking/Session/ServerSlotDirectory.cs"
tags: [clase, modulo/networking]
---
# ServerSlotDirectory

> Server-side seat table: which client holds Player1 / Player2. Plain C#, owned by the approval handler and shared with the roster through the runtime anchor. Version 1.5: a seat is freed as soon as its client disconnects.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ServerSlotDirectory.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ServerSlotDirectory {
    +int ConnectedCount
    +TryAssign(ulong clientId, ConnectionPayload payload, PlayerId id) bool
    +TryGetByClient(ulong clientId, PlayerSlot slot) bool
    +TryGet(PlayerId id, PlayerSlot slot) bool
    +Release(ulong clientId, PlayerId id) bool
    +Clear() void
  }
  class PlayerSlot:::external
  <<struct>> PlayerSlot
  ServerSlotDirectory --> PlayerSlot
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ConnectionPayload]], [[PlayerId]], [[PlayerSlot]]

## Usado por
[[ConnectionApprovalHandler]], [[NetworkPlayerRoster]], [[NetworkRuntimeAnchor]]
