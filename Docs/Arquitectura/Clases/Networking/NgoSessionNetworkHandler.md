---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: class
archivo: "Assets/Scripts/Networking/Session/NgoSessionNetworkHandler.cs"
tags: [clase, modulo/networking]
---
# NgoSessionNetworkHandler

> Sessions network handler for this project's NetworkManager: replaces the SDK's default NGO integration (which uses NetworkManager.Singleton and warns when NGO is shut down outside the session) so the injected root's manager and transport are used, the connection payload/approval written by the caller stay in charge, and the root can shut NGO down itself (host disconnect reasons). One instance per session start; it only ever shuts down an NGO run it started itself, so a stale handler never stops a newer session. Single use: once a start failed, was cancelled, or NGO was stopped, the handler is `Retired` and any later `StartAsync(NetworkConfiguration, CancellationToken)` returns without starting NGO. SDK 2.x (NetworkModule) clears its network property when a client join fails and, on the next ISession.Changed raised while the failed session is torn down, joins the network again with this same handler from an async void: that start must neither run (ghost connection to the host) nor throw (unobserved exception).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/NgoSessionNetworkHandler.cs`

## Hereda / implementa
`INetworkHandler`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NgoSessionNetworkHandler {
    +string DisconnectReason
    +bool Started
    +bool Retired
    +StartAsync(NetworkConfiguration configuration) Task
    +StartAsync(NetworkConfiguration configuration, CancellationToken cancellationToken) Task
    +Retire() void
    +StopAsync() Task
    +Shutdown() bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[RelaySessionConnection]], [[RelaySessionStart]]
