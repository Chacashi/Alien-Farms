---
modulo: Audio
namespace: AlienFarms.Audio
tipo: class
archivo: "Assets/Scripts/Audio/SfxVoicePool.cs"
tags: [clase, modulo/audio]
---
# SfxVoicePool

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/SfxVoicePool.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SfxVoicePool {
    +Play(SoundCue cue, IRandomSource rng, bool ignoreListenerPause) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AudioVariation]], [[IRandomSource]], [[SoundCue]]

## Usado por
[[CharacterSelectSfxPlayer]], [[MatchStatusSfxPlayer]], [[OnlineSessionSfxPlayer]]
