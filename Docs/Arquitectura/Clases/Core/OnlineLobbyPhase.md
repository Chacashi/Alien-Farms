---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/OnlineLobbyPhase.cs"
tags: [clase, modulo/core]
---
# OnlineLobbyPhase

> Phase of the online room (character pick before the match) as published by the network lobby.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/OnlineLobbyPhase.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineLobbyPhase {
    <<enumeration>>
    Unavailable
    Open
    Starting
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Unavailable`, `Open`, `Starting`

## Usado por
[[LobbyStateSpawner]], [[NetworkLobbyState]], [[OnlineLobbyChannel]], [[OnlineLobbySnapshot]], [[OnlineMenuController]], [[OnlineMenuRules]], [[OnlineSessionSfxPlayer]]
