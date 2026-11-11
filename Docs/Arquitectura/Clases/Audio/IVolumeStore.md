---
modulo: Audio
namespace: AlienFarms.Audio
tipo: interface
archivo: "Assets/Scripts/Audio/IVolumeStore.cs"
tags: [clase, modulo/audio]
---
# IVolumeStore

> Key/value persistence for volume preferences (PlayerPrefs at runtime, a fake in tests).

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/IVolumeStore.cs`

## Derivados / implementaciones
[[PlayerPrefsVolumeStore]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IVolumeStore {
    <<interface>>
    +GetFloat(string key, float defaultValue) float
    +SetFloat(string key, float value) void
    +GetBool(string key, bool defaultValue) bool
    +SetBool(string key, bool value) void
    +Save() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PlayerPrefsVolumeStore]], [[VolumeState]]
