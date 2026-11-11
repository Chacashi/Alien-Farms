---
modulo: Match
namespace: AlienFarms.Match
tipo: static
archivo: "Assets/Scripts/Match/TimeFormat.cs"
tags: [clase, modulo/match]
---
# TimeFormat

> Allocation-free time formatting into reusable char buffers.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/TimeFormat.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class TimeFormat {
    <<static>>
    +WriteMinutesSeconds(int totalSeconds, char buffer)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[MatchTimerView]]
