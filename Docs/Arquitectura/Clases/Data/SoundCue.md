---
modulo: Data
namespace: AlienFarms.Data
tipo: class
archivo: "Assets/Scripts/Data/SoundCue.cs"
tags: [clase, modulo/data]
---
# SoundCue

> One sound effect: candidate clips (one picked at random), volume and random pitch range.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/SoundCue.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SoundCue {
    -AudioClip[] _clips
    -float _volume
    -Vector2 _pitchRange
    +AudioClip[] Clips
    +float Volume
    +Vector2 PitchRange
    +bool HasClips
    +Sanitize() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[AnimalData]], [[AudioVariation]], [[BombData]], [[BombSfxPlayer]]
