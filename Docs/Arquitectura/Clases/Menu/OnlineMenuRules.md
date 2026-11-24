---
modulo: Menu
namespace: AlienFarms.Menu
tipo: static
archivo: "Assets/Scripts/Menu/OnlineMenuRules.cs"
tags: [clase, modulo/menu]
---
# OnlineMenuRules

> Pure rules of the online menu: which screen the session and lobby state lead to, and which localization keys the busy label and the room hint show. Shared by `OnlineMenuController` and its tests.

**Módulo:** [[Modulo Menu|Menu]] · **Namespace:** `AlienFarms.Menu` · **Archivo:** `Assets/Scripts/Menu/OnlineMenuRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineMenuRules {
    <<static>>
    +Resolve(OnlineSessionStatus status, OnlineLobbyPhase lobbyPhase, bool wantsJoin)$ OnlineScreen
    +BusyKey(OnlineSessionStatus status, OnlineLobbyPhase lobbyPhase)$ string
    +CanCancelBusy(OnlineSessionStatus status)$ bool
    +OpensOnlineOnLoad(bool inOnlineSession, bool returnToOnlineRequested)$ bool
    +WantsJoinAfterCancelBusy(OnlineSessionStatus status, bool wantsJoin)$ bool
    +ShowsRejoin(bool hasOffer, double expiresAt, double now)$ bool
    +RejoinSecondsLeft(double expiresAt, double now)$ int
    +HintKey(OnlineLobbyPhase lobbyPhase, int seatCount, bool allPicked, bool isHost)$ string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LocalizationKeys]], [[OnlineLobbyChannel]], [[OnlineLobbyPhase]], [[OnlineScreen]], [[OnlineSessionStatus]]

## Usado por
[[MenuFlowController]], [[OnlineMenuController]]
