---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/MatchSettings.cs"
tags: [clase, modulo/data]
---
# MatchSettings

> Timing rules of a match round.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/MatchSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchSettings {
    <<ScriptableObject>>
    -float _matchDuration
    -int _countdownSeconds
    -float _goMessageDuration
    -float _resultsInputDelay
    +float MatchDuration
    +int CountdownSeconds
    +float GoMessageDuration
    +float ResultsInputDelay
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[IMatchContext]], [[MatchManager]]
