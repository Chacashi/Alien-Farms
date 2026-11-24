---
modulo: Audio
namespace: AlienFarms.Audio
tipo: MonoBehaviour
archivo: "Assets/Scripts/Audio/MatchStatusSfxPlayer.cs"
tags: [clase, modulo/audio]
---
# MatchStatusSfxPlayer

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/MatchStatusSfxPlayer.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchStatusSfxPlayer {
    <<MonoBehaviour>>
    -UiSfxLibrary _library
    -AudioSource[] _voices
  }
  class IRandomSource:::external
  <<interface>> IRandomSource
  class MatchConnectionCueTracker:::external
  class SfxVoicePool:::external
  class UiSfxLibrary:::external
  <<ScriptableObject>> UiSfxLibrary
  MatchStatusSfxPlayer --> IRandomSource
  MatchStatusSfxPlayer --> MatchConnectionCueTracker
  MatchStatusSfxPlayer --> SfxVoicePool
  MatchStatusSfxPlayer --> UiSfxLibrary
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[IRandomSource]], [[MatchConnectionCueTracker]], [[MatchPhase]], [[PlayerConnectionStatus]], [[PlayerId]], [[SfxVoicePool]], [[SystemRandomSource]], [[UiSfxCue]], [[UiSfxLibrary]]

## Eventos que escucha
[[Eventos GameEvents#OnGamePaused|OnGamePaused]], [[Eventos GameEvents#OnMatchPhaseChanged|OnMatchPhaseChanged]], [[Eventos GameEvents#OnPauseMenuResumed|OnPauseMenuResumed]], [[Eventos GameEvents#OnPlayerConnectionChanged|OnPlayerConnectionChanged]]
