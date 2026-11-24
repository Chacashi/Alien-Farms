---
modulo: Audio
namespace: AlienFarms.Audio
tipo: static
archivo: "Assets/Scripts/Audio/OnlineSessionCueRules.cs"
tags: [clase, modulo/audio]
---
# OnlineSessionCueRules

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/OnlineSessionCueRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineSessionCueRules {
    <<static>>
    +OnStatusChanged(OnlineSessionStatus prev, OnlineSessionStatus curr)$ UiSfxCue
    +IsArrivalNotice(OnlineSessionStatus status, string errorKey)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[OnlineSessionErrorKeys]], [[OnlineSessionStatus]], [[UiSfxCue]]

## Usado por
[[OnlineSessionSfxPlayer]]
