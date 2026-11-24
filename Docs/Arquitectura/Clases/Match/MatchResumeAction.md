---
modulo: Match
namespace: AlienFarms.Match
tipo: enumeration
archivo: "Assets/Scripts/Match/MatchResumeAction.cs"
tags: [clase, modulo/match]
---
# MatchResumeAction

> What the match does when a suspension ends (see `MatchSuspendRules.OnResume`).

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/MatchResumeAction.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchResumeAction {
    <<enumeration>>
    None
    EnableControls
    RestartCountdown
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`None`, `EnableControls`, `RestartCountdown`

## Usado por
[[MatchManager]], [[MatchSuspendRules]]
