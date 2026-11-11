---
modulo: Audio
namespace: AlienFarms.Audio
tipo: static
archivo: "Assets/Scripts/Audio/VolumeMath.cs"
tags: [clase, modulo/audio]
---
# VolumeMath

> Conversions between linear slider volume (0..1) and AudioMixer attenuation (dB).

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/VolumeMath.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class VolumeMath {
    <<static>>
    +LinearToDecibels(float linear)$ float
    +DecibelsToLinear(float decibels)$ float
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[VolumeState]]
