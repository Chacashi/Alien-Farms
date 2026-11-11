---
modulo: Audio
namespace: AlienFarms.Audio
tipo: static
archivo: "Assets/Scripts/Audio/AudioVariation.cs"
tags: [clase, modulo/audio]
---
# AudioVariation

> Pure helpers that add variety to repeated sounds (random clip and pitch, voice selection).

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/AudioVariation.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AudioVariation {
    <<static>>
    +PickRandom(T items, IRandomSource rng)$ T
    +PickPitch(Vector2 range, IRandomSource rng)$ float
    +RampPitch(float basePitch, float elapsed, float rampTime, float multiplier)$ float
    +SelectVoice(bool busy, int next)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IRandomSource]], [[SoundCue]]

## Usado por
[[AnimalSfxPlayer]], [[BombSfxPlayer]]
