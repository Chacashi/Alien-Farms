---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: class
archivo: "Assets/Scripts/Networking/Session/ServerStopLatch.cs"
tags: [clase, modulo/networking]
---
# ServerStopLatch

> "The host is leaving" state of the server's connection handling (plain C#, owned by `ConnectionApprovalHandler`). Requested when the root starts the host's leave; from then the run's disconnections are the shutdown, not drops. It lasts exactly one run: cleared when NGO reports the server stopped (the seat table is cleared in the same step, so a late disconnection finds nothing to reserve) and again when a new session starts, since the root survives a Leave in the lobby and is reused for the next room.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ServerStopLatch.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ServerStopLatch {
    +bool IsRequested
    +Request(bool isServer) bool
    +OnServerStopped() void
    +Reset() void
    +ShouldRejectJoin(ulong clientId, bool serverStopping)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ConnectionApprovalHandler]]
