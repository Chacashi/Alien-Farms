---
modulo: Core
namespace: AlienFarms.Core
tipo: struct
archivo: "Assets/Scripts/Core/MatchResult.cs"
tags: [clase, modulo/core]
---
# MatchResult

> Final outcome of a round. `Winner` is `PlayerId.None` on a draw.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/MatchResult.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchResult {
    <<struct>>
    +PlayerId Winner
    +int Player1Score
    +int Player2Score
    +bool IsDraw
    +GetScore(PlayerId id) int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]]

## Usado por
[[GameEvents]], [[MatchResultsView]], [[MatchScore]], [[ResultsMenuController]]
