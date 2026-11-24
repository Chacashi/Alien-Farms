---
modulo: Audio
namespace: AlienFarms.Audio
tipo: class
archivo: "Assets/Scripts/Audio/MatchConnectionCueTracker.cs"
tags: [clase, modulo/audio]
---
# MatchConnectionCueTracker

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/MatchConnectionCueTracker.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchConnectionCueTracker {
    +SetPhase(MatchPhase phase) void
    +Reset() void
    +Apply(PlayerId id, PlayerConnectionStatus status, Span~UiSfxCue~ output, bool isLocalSeat) int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchPhase]], [[MatchSuspendRules]], [[PlayerConnectionStatus]], [[PlayerId]], [[UiSfxCue]]

## Usado por
[[MatchStatusSfxPlayer]]
