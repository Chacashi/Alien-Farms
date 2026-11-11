---
modulo: Audio
namespace: AlienFarms.Audio
tipo: class
archivo: "Assets/Scripts/Audio/VolumeState.cs"
tags: [clase, modulo/audio]
---
# VolumeState

> Plain C# volume model: linear level per `VolumeChannel` plus a master mute, loaded from and written through an `IVolumeStore`. Missing keys fall back to `AudioVolumeSettings` defaults.

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/VolumeState.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class VolumeState {
    +bool IsMuted
    +GetLevel(VolumeChannel channel) float
    +SetLevel(VolumeChannel channel, float level) bool
    +SetMuted(bool muted) bool
    +GetAppliedDecibels(VolumeChannel channel) float
    +Save() void
    +GetKey(VolumeChannel channel)$ string
  }
  class IVolumeStore:::external
  <<interface>> IVolumeStore
  VolumeState --> IVolumeStore
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AudioMixerParameters]], [[AudioVolumeSettings]], [[IVolumeStore]], [[VolumeChannel]], [[VolumeMath]]

## Usado por
[[AudioVolumeService]]
