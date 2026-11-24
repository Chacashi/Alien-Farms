---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: static
archivo: "Assets/Scripts/Networking/Session/RejoinOfferRules.cs"
tags: [clase, modulo/networking]
---
# RejoinOfferRules

> Pure rules of the client's reconnection offer: when a drop arms it, until when it lasts and whether a failed rejoin keeps it for another try. Times are Time.realtimeSinceStartupAsDouble seconds.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/RejoinOfferRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class RejoinOfferRules {
    <<static>>
    +ShouldOffer(bool isHost, string errorKey, string sessionId, string code)$ bool
    +ExpiresAt(double now, float graceSeconds)$ double
    +IsExpired(double expiresAt, double now)$ bool
    +KeepsOfferAfterFailure(string errorKey)$ bool
    +ShouldAutoRejoin(bool hasOffer, OnlineSessionStatus status, bool networkReachable, double now, double nextAttemptAt)$ bool
    +FallsBackToCodeJoin(string reconnectErrorKey)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[OnlineSessionErrorKeys]], [[OnlineSessionStatus]]

## Usado por
[[OnlineSessionService]], [[RelaySessionConnection]], [[SessionExit]]
