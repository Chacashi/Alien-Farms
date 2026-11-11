---
modulo: Match
namespace: AlienFarms.Match
tipo: class
archivo: "Assets/Scripts/Match/LocalMatchExitHandler.cs"
tags: [clase, modulo/match]
---
# LocalMatchExitHandler

> Local split-screen exit: rematch through the `MatchManager`; returning to the title clears the `MatchSession` and loads the main menu (time and audio are restored by `SceneFlow`).

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/LocalMatchExitHandler.cs`

## Hereda / implementa
[[IMatchExitHandler]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class LocalMatchExitHandler {
    +RequestRematch() void
    +ReturnToTitle() bool
  }
  class IMatchExitHandler:::external
  <<interface>> IMatchExitHandler
  class MatchManager:::external
  <<MonoBehaviour>> MatchManager
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  LocalMatchExitHandler --> MatchManager
  LocalMatchExitHandler --> MatchSession
  LocalMatchExitHandler ..|> IMatchExitHandler
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IMatchExitHandler]], [[MatchManager]], [[MatchSession]], [[SceneFlow]]

## Usado por
[[MatchExit]]
