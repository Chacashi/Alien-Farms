---
modulo: Core
namespace: AlienFarms.Core
tipo: struct
archivo: "Assets/Scripts/Core/OnlineLobbySeat.cs"
tags: [clase, modulo/core]
---
# OnlineLobbySeat

> One player seat of the online room as seen by the menu (published by the network lobby).

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/OnlineLobbySeat.cs`

## Hereda / implementa
`IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineLobbySeat {
    <<struct>>
    +PlayerId Id
    +int CharacterIndex
    +string DisplayName
    +bool IsLocal
    +bool IsHost
    +bool HasCharacter
    +Equals(OnlineLobbySeat other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]]

## Usado por
[[NetworkLobbyState]], [[OnlineLobbyChannel]], [[OnlineLobbyCueRules]], [[OnlineLobbySnapshot]], [[OnlineMenuController]], [[OnlinePanelView]], [[OnlineSeatView]]
