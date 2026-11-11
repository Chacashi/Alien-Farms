---
modulo: Match
namespace: AlienFarms.Match
tipo: static
archivo: "Assets/Scripts/Match/PauseRules.cs"
tags: [clase, modulo/match]
---
# PauseRules

> Pure rules for when the game may be paused.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/PauseRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PauseRules {
    <<static>>
    +CanPause(MatchPhase phase)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchPhase]]

## Usado por
[[PauseController]], [[PauseHoldIndicatorView]]
