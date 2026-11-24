---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: enumeration
archivo: "Assets/Scripts/Networking/Session/OnlineSessionService.cs"
tags: [clase, modulo/networking]
---
# SessionExit

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/OnlineSessionService.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SessionExit {
    <<enumeration>>
    Leave
    AwaitHostEnd
    Forget
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Leave`, `AwaitHostEnd`, `Forget`

## Usa
[[ConnectionApprovalHandler]], [[ConnectionPayload]], [[DisplayNameResolver]], [[DisplayNameSanitizer]], [[GameMode]], [[MatchSession]], [[NetworkRootLifetime]], [[NetworkRuntimeAnchor]], [[NetworkSettings]], [[OnlineModeRules]], [[OnlineParticipant]], [[OnlineSessionChannel]], [[OnlineSessionErrorKeys]], [[OnlineSessionErrorMapper]], [[OnlineSessionStatus]], [[PlayerId]], [[RejoinOfferRules]], [[RelaySessionConnection]], [[RelaySessionStart]], [[Status]], [[UgsBootstrapper]]
