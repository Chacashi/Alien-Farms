---
modulo: Audio
namespace: AlienFarms.Audio
tipo: MonoBehaviour
archivo: "Assets/Scripts/Audio/AudioVolumeService.cs"
tags: [clase, modulo/audio]
---
# AudioVolumeService

> Per-scene volume service: loads the saved levels (PlayerPrefs) and drives the exposed volume parameters of the AudioMixer. Not a singleton: each scene has one and options views reference it in the Inspector.

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/AudioVolumeService.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AudioVolumeService {
    <<MonoBehaviour>>
    -AudioMixer _mixer
    -AudioVolumeSettings _settings
    +bool IsMuted
    +GetLevel(VolumeChannel channel) float
    +SetLevel(VolumeChannel channel, float level) void
    +SetMuted(bool muted) void
    +Save() void
  }
  class AudioVolumeSettings:::external
  <<ScriptableObject>> AudioVolumeSettings
  class VolumeState:::external
  AudioVolumeService --> AudioVolumeSettings
  AudioVolumeService --> VolumeState
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AudioMixerParameters]], [[AudioVolumeSettings]], [[PlayerPrefsVolumeStore]], [[VolumeChannel]], [[VolumeState]]

## Usado por
[[AudioOptionsView]]
