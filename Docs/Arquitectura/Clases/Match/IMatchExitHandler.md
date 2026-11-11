---
modulo: Match
namespace: AlienFarms.Match
tipo: interface
archivo: "Assets/Scripts/Match/IMatchExitHandler.cs"
tags: [clase, modulo/match]
---
# IMatchExitHandler

> What the match exit buttons do. Local: `LocalMatchExitHandler`; online, a network handler.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/IMatchExitHandler.cs`

## Derivados / implementaciones
[[LocalMatchExitHandler]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IMatchExitHandler {
    <<interface>>
    +RequestRematch() void
    +ReturnToTitle() bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[LocalMatchExitHandler]], [[MatchExit]]
