---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: class
archivo: "Assets/Scripts/Networking/Session/RelaySessionStart.cs"
tags: [clase, modulo/networking]
---
# RelaySessionStart

> Outcome of one create / join through `RelaySessionConnection`: the session and the network handler of that start only. Late cleanup (a start that finished after a Leave or a root teardown) acts on this instance, never on whatever session is current by then.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/RelaySessionStart.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class RelaySessionStart {
    +ISession Session
    +NgoSessionNetworkHandler Handler
    +string ErrorKey
    +string LeftSessionId
    +bool IsSuccess
    +WithLeftSession(string sessionId) RelaySessionStart
    +Succeeded(ISession session, NgoSessionNetworkHandler handler)$ RelaySessionStart
    +Failed(string errorKey)$ RelaySessionStart
  }
  class NgoSessionNetworkHandler:::external
  RelaySessionStart --> NgoSessionNetworkHandler
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[NgoSessionNetworkHandler]]

## Usado por
[[OnlineSessionService]], [[RelaySessionConnection]], [[SessionExit]]
