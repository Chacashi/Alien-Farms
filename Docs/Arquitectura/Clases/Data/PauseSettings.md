---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/PauseSettings.cs"
tags: [clase, modulo/data]
---
# PauseSettings

> Hold-to-pause tuning.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/PauseSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PauseSettings {
    <<ScriptableObject>>
    -float _holdDuration
    -float _showProgressAfter
    +float HoldDuration
    +float ShowProgressAfter
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PauseHoldIndicatorView]], [[PlayerPauseInput]]
