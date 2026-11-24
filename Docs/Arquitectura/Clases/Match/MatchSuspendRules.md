---
modulo: Match
namespace: AlienFarms.Match
tipo: static
archivo: "Assets/Scripts/Match/MatchSuspendRules.cs"
tags: [clase, modulo/match]
---
# MatchSuspendRules

> Pure rules of a match suspension (online reconnection grace): which phases are suspended and what to do when the suspension ends.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/MatchSuspendRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchSuspendRules {
    <<static>>
    +SuspendsIn(MatchPhase phase)$ bool
    +AllowsRematch(bool suspended, bool stateReady)$ bool
    +OnResume(MatchPhase phase, bool playersComplete)$ MatchResumeAction
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchPhase]], [[MatchResumeAction]]

## Usado por
[[MatchConnectionCueTracker]], [[MatchManager]], [[ReconnectGraceCoordinator]]
