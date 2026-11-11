---
modulo: Audio
namespace: AlienFarms.Audio
tipo: class
archivo: "Assets/Scripts/Audio/PlayerPrefsVolumeStore.cs"
tags: [clase, modulo/audio]
---
# PlayerPrefsVolumeStore

> `IVolumeStore` backed by `PlayerPrefs` (bools stored as 0/1 ints).

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/PlayerPrefsVolumeStore.cs`

## Hereda / implementa
[[IVolumeStore]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerPrefsVolumeStore {
    +GetFloat(string key, float defaultValue) float
    +SetFloat(string key, float value) void
    +GetBool(string key, bool defaultValue) bool
    +SetBool(string key, bool value) void
    +Save() void
  }
  class IVolumeStore:::external
  <<interface>> IVolumeStore
  PlayerPrefsVolumeStore ..|> IVolumeStore
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IVolumeStore]]

## Usado por
[[AudioVolumeService]]
