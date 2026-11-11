---
modulo: Match
namespace: AlienFarms.Match
tipo: MonoBehaviour
archivo: "Assets/Scripts/Match/MatchExit.cs"
tags: [clase, modulo/match]
---
# MatchExit

> Exit options of a finished (or paused) match, wired to menu buttons: rematch or back to the title screen. What they do is delegated to an `IMatchExitHandler`: the local one by default, or one set with `SetHandler`.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/MatchExit.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchExit {
    <<MonoBehaviour>>
    -MatchManager _match
    -MatchSession _session
    +SetHandler(IMatchExitHandler handler) void
    +Rematch() void
    +ReturnToTitle() void
  }
  class IMatchExitHandler:::external
  <<interface>> IMatchExitHandler
  class MatchManager:::external
  <<MonoBehaviour>> MatchManager
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  MatchExit --> IMatchExitHandler
  MatchExit --> MatchManager
  MatchExit --> MatchSession
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IMatchExitHandler]], [[LocalMatchExitHandler]], [[MatchManager]], [[MatchSession]]

## Usado por
[[PauseController]]
