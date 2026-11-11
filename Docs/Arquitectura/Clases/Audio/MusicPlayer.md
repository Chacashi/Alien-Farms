---
modulo: Audio
namespace: AlienFarms.Audio
tipo: MonoBehaviour
archivo: "Assets/Scripts/Audio/MusicPlayer.cs"
tags: [clase, modulo/audio]
---
# MusicPlayer

> Loops a scene's music track on a 2D AudioSource (route its output to the Music mixer group). Optionally keeps playing while the game is paused (`AudioListener.pause`).

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/MusicPlayer.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MusicPlayer {
    <<MonoBehaviour>>
    -AudioClip _track
    -bool _playDuringPause
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```
