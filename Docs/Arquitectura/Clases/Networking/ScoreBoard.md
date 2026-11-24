---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: struct
archivo: "Assets/Scripts/Networking/Match/ScoreBoard.cs"
tags: [clase, modulo/networking]
---
# ScoreBoard

> Both players' published scores. The match result is derived from it (no result message).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/ScoreBoard.cs`

## Hereda / implementa
`INetworkSerializable`, `IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ScoreBoard {
    <<struct>>
    +int Player1
    +int Player2
    +Get(PlayerId id) int
    +With(PlayerId id, int score) ScoreBoard
    +ToResult() MatchResult
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(ScoreBoard other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchResult]], [[PlayerId]]

## Usado por
[[NetworkMatchState]]
