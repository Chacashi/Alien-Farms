---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: enumeration
archivo: "Assets/Scripts/Networking/Session/LobbyRequestResult.cs"
tags: [clase, modulo/networking]
---
# LobbyRequestResult

> Outcome of a character request in the online room (see `LobbyRules.EvaluateRequest`).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/LobbyRequestResult.cs`

## Hereda / implementa
`byte`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LobbyRequestResult {
    <<enumeration>>
    Accepted
    Released
    Unchanged
    RejectedNotSeated
    RejectedOutOfRange
    RejectedTaken
    RejectedClosed
    RejectedLocked
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Accepted`, `Released`, `Unchanged`, `RejectedNotSeated`, `RejectedOutOfRange`, `RejectedTaken`, `RejectedClosed`, `RejectedLocked`

## Usado por
[[LobbyRules]], [[NetworkLobbyState]]
