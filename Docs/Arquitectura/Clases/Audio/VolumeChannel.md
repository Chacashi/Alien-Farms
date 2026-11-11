---
modulo: Audio
namespace: AlienFarms.Audio
tipo: enumeration
archivo: "Assets/Scripts/Audio/VolumeChannel.cs"
tags: [clase, modulo/audio]
---
# VolumeChannel

> Volume channels exposed by the AlienFarmsMixer. Values index per-channel arrays.

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/VolumeChannel.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class VolumeChannel {
    <<enumeration>>
    Master
    Music
    Sfx
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Master`, `Music`, `Sfx`

## Usado por
[[AudioMixerParameters]], [[AudioOptionsView]], [[AudioVolumeService]], [[AudioVolumeSettings]], [[VolumeState]]
