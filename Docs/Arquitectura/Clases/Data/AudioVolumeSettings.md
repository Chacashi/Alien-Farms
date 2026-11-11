---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/AudioVolumeSettings.cs"
tags: [clase, modulo/data]
---
# AudioVolumeSettings

> Default volume levels used until the player changes them (saved choices live in PlayerPrefs).

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/AudioVolumeSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AudioVolumeSettings {
    <<ScriptableObject>>
    -float _defaultMaster
    -float _defaultMusic
    -float _defaultSfx
    -bool _defaultMuted
    +bool DefaultMuted
    +GetDefaultLevel(VolumeChannel channel) float
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[VolumeChannel]]

## Usado por
[[AudioVolumeService]], [[VolumeState]]
