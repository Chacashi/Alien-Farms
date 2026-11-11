---
modulo: Audio
namespace: AlienFarms.Audio
tipo: static
archivo: "Assets/Scripts/Audio/AudioMixerParameters.cs"
tags: [clase, modulo/audio]
---
# AudioMixerParameters

> Names of the exposed AudioMixer parameters (volume of each group, in dB). The only place these strings live: they must match the exposed parameters of AlienFarmsMixer exactly.

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/AudioMixerParameters.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AudioMixerParameters {
    <<static>>
    +GetVolumeParameter(VolumeChannel channel)$ string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[VolumeChannel]]

## Usado por
[[AudioVolumeService]], [[VolumeState]]
