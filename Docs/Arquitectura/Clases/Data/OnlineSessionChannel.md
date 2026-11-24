---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/OnlineSessionChannel.cs"
tags: [clase, modulo/data]
---
# OnlineSessionChannel

> Runtime channel between the online menu (requests, views) and the session service on the NetworkRoot (state). The menu only calls the Request* methods and reads the state; the service listens to the requests and is the only writer. Primitive data only, so it lives in the runtime assembly and the menu never references networking. Nothing is serialized: state from a previous play session is discarded through a generation counter.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/OnlineSessionChannel.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineSessionChannel {
    <<ScriptableObject>>
    +event Action Changed
    +event Action SignInRequested
    +event Action CreateRequested
    +event Action~string~ JoinRequested
    +event Action LeaveRequested
    +event Action RejoinRequested
    +event Action DeclineRejoinRequested
    +OnlineSessionStatus Status
    +bool IsRejoining
    +string Code
    +bool IsHost
    +string LocalPlayerId
    +string LocalPlayerName
    ...25 más
  }
  class OnlineParticipant:::external
  <<struct>> OnlineParticipant
  OnlineSessionChannel --> OnlineParticipant
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[DisplayNameSanitizer]], [[OnlineParticipant]], [[OnlineSessionErrorKeys]], [[OnlineSessionStatus]], [[OnlineSessionStatusRules]], [[SessionCodeFormat]], [[Status]]

## Usado por
[[CloudSaveStatsService]], [[DevConnectionPanel]], [[LobbyStateSpawner]], [[MenuFlowController]], [[NetworkLobbyState]], [[OnlineMenuController]], [[OnlineSessionService]], [[OnlineSessionSfxPlayer]], [[PlayerStatsSync]], [[SessionExit]], [[Status]]
