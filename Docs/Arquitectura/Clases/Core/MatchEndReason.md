---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/MatchEndReason.cs"
tags: [clase, modulo/core]
---
# MatchEndReason

> Why a round ended. Replicated as a byte, so values must stay stable.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/MatchEndReason.cs`

## Hereda / implementa
`byte`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchEndReason {
    <<enumeration>>
    TimeUp
    Forfeit
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`TimeUp`, `Forfeit`

## Usado por
[[MatchResult]], [[MatchResultRules]]
