---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: static
archivo: "Assets/Scripts/Networking/Session/OnlineSessionErrorMapper.cs"
tags: [clase, modulo/networking]
---
# OnlineSessionErrorMapper

> Maps Unity Gaming Services failures and NGO disconnect reasons to `OnlineSessionErrorKeys`. Pure. Note: the Sessions SDK reports some lobby failures (session full, malformed code) as `SessionError.Unknown` without an inner exception; for a join a full session is recognized by its message (`IsSessionFull`) and anything else surfaces as `OnlineSessionErrorKeys.JoinFailed` (see `FromJoinException`).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/OnlineSessionErrorMapper.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineSessionErrorMapper {
    <<static>>
    +FromSessionError(SessionError error)$ string
    +FromRequestErrorCode(int errorCode, string fallbackKey)$ string
    +FromRelayReason(RelayExceptionReason reason)$ string
    +FromJoinException(Exception exception)$ string
    +IsSessionFull(Exception exception)$ bool
    +FromDisconnectReason(string reason)$ string
    +FromException(Exception exception, string fallbackKey)$ string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[DisconnectReasons]], [[OnlineSessionErrorKeys]]

## Usado por
[[OnlineSessionService]], [[RelaySessionConnection]], [[SessionExit]], [[UgsBootstrapper]]
