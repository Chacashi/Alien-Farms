---
modulo: Audio
namespace: AlienFarms.Audio
tipo: struct
archivo: "Assets/Scripts/Audio/OnlineLobbySnapshot.cs"
tags: [clase, modulo/audio]
---
# OnlineLobbySnapshot

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/OnlineLobbySnapshot.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineLobbySnapshot {
    <<struct>>
    +OnlineLobbyPhase Phase
    +int SeatCount
    +OnlineLobbySeat Seat0
    +OnlineLobbySeat Seat1
    +bool HasLocalSeat
    +bool IsInRoom
    +Capture(OnlineLobbyChannel channel)$ OnlineLobbySnapshot
    +TryGetSeat(PlayerId id, OnlineLobbySeat seat) bool
  }
  class OnlineLobbySeat:::external
  <<struct>> OnlineLobbySeat
  OnlineLobbySnapshot --> OnlineLobbySeat
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[OnlineLobbyChannel]], [[OnlineLobbyPhase]], [[OnlineLobbySeat]], [[Phase]], [[PlayerId]]

## Usado por
[[OnlineLobbyCueRules]], [[OnlineSessionSfxPlayer]]
