---
modulo: Match
namespace: AlienFarms.Match
tipo: class
archivo: "Assets/Scripts/Match/MatchTimer.cs"
tags: [clase, modulo/match]
---
# MatchTimer

> Countdown of the round time. Driven externally with `Tick` (no Time.* access).

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/MatchTimer.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchTimer {
    +float Remaining
    +bool IsFinished
    +int DisplaySeconds
    +Reset(float duration) void
    +Tick(float deltaTime) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[IMatchContext]], [[MatchManager]], [[PlayingState]]
