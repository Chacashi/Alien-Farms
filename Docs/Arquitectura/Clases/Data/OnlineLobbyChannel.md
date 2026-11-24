---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/OnlineLobbyChannel.cs"
tags: [clase, modulo/data]
---
# OnlineLobbyChannel

> Runtime channel between the online room menu (requests, views) and the network lobby (state). The menu only calls the Request* methods and reads the state; the network lobby listens to the requests on every peer and is the only writer. Primitive data only, so the menu never references networking. Nothing is serialized: state from a previous play session is discarded through a generation counter.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/OnlineLobbyChannel.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineLobbyChannel {
    <<ScriptableObject>>
    +event Action Changed
    +event Action~int~ CharacterRequested
    +event Action StartRequested
    +OnlineLobbyPhase Phase
    +int SeatCount
    +PlayerId LocalPlayerId
    +bool CanStart
    +string LastRejectKey
    +bool HasReject
    +GetSeat(int index) OnlineLobbySeat
    +TryGetSeat(PlayerId id, OnlineLobbySeat seat) bool
    +RequestCharacter(int characterIndex) bool
    +RequestStart() bool
    ...4 más
  }
  class OnlineLobbySeat:::external
  <<struct>> OnlineLobbySeat
  OnlineLobbyChannel --> OnlineLobbySeat
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[OnlineLobbyPhase]], [[OnlineLobbySeat]], [[OnlineSessionErrorKeys]], [[Phase]], [[PlayerId]]

## Usado por
[[DevConnectionPanel]], [[LobbyStateSpawner]], [[NetworkLobbyState]], [[OnlineLobbySnapshot]], [[OnlineMenuController]], [[OnlineMenuRules]], [[OnlinePanelView]], [[OnlineSessionSfxPlayer]], [[Status]]
