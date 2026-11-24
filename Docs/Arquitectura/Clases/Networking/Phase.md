---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: enumeration
archivo: "Assets/Scripts/Networking/Session/HostShutdownSequence.cs"
tags: [clase, modulo/networking]
---
# Phase

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/HostShutdownSequence.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class Phase {
    <<enumeration>>
    Idle
    Announcing
    Disconnecting
    Stopping
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Idle`, `Announcing`, `Disconnecting`, `Stopping`

## Usa
[[HostShutdownStep]]

## Usado por
[[CountdownState]], [[HostShutdownSequence]], [[MatchState]], [[MatchStateMachine]], [[MenuFlowController]], [[NetworkMatchState]], [[OnlineLobbyChannel]], [[OnlineLobbySnapshot]], [[OnlineMenuController]], [[OnlineSessionSfxPlayer]], [[PlayingState]], [[ResultsState]], [[WaitingForPlayersState]]
