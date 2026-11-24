---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: class
archivo: "Assets/Scripts/Networking/Session/UgsBootstrapper.cs"
tags: [clase, modulo/networking]
---
# UgsBootstrapper

> Initializes Unity Gaming Services (environment from NetworkSettings, one Authentication profile per Multiplayer Play Mode instance, see `UgsProfileResolver`) and signs in anonymously. Idempotent: concurrent calls share the pending attempt and a signed-in player returns at once; after a failure the next call retries. Plain class owned by `OnlineSessionService`; the UGS services themselves are global.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/UgsBootstrapper.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class UgsBootstrapper {
    +string Profile
    +string LastErrorKey
    +bool IsSignedIn
    +IAuthenticationService Authentication
    +string PlayerId
    +string PlayerName
    +ResolveProcessProfile()$ string
    +EnsureSignedInAsync() Task~bool~
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[OnlineSessionErrorKeys]], [[OnlineSessionErrorMapper]], [[PlayerId]], [[UgsProfileResolver]]

## Usado por
[[DevConnectionPanel]], [[OnlineSessionService]], [[SessionExit]], [[Status]]
