---
modulo: Audio
namespace: AlienFarms.Audio
tipo: static
archivo: "Assets/Scripts/Audio/OnlineLobbyCueRules.cs"
tags: [clase, modulo/audio]
---
# OnlineLobbyCueRules

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/OnlineLobbyCueRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineLobbyCueRules {
    <<static>>
    +Diff(OnlineLobbySnapshot prev, OnlineLobbySnapshot curr)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[OnlineLobbySeat]], [[OnlineLobbySnapshot]], [[PlayerId]], [[UiSfxCue]]

## Usado por
[[OnlineSessionSfxPlayer]]
