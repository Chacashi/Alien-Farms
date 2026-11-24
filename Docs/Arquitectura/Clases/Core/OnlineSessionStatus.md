---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/OnlineSessionStatus.cs"
tags: [clase, modulo/core]
---
# OnlineSessionStatus

> Lifecycle of the online session as seen by this machine (see `OnlineSessionStatusRules`).

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/OnlineSessionStatus.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineSessionStatus {
    <<enumeration>>
    SignedOut
    SigningIn
    Ready
    Creating
    Joining
    InSession
    Leaving
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`SignedOut`, `SigningIn`, `Ready`, `Creating`, `Joining`, `InSession`, `Leaving`

## Usado por
[[DevConnectionPanel]], [[LobbyStateSpawner]], [[MenuFlowController]], [[OnlineMenuController]], [[OnlineMenuRules]], [[OnlineModeRules]], [[OnlineSessionChannel]], [[OnlineSessionCueRules]], [[OnlineSessionService]], [[OnlineSessionSfxPlayer]], [[OnlineSessionStatusRules]], [[PlayerStatsSync]], [[RejoinOfferRules]], [[SessionExit]], [[Status]]
