---
modulo: Audio
namespace: AlienFarms.Audio
tipo: MonoBehaviour
archivo: "Assets/Scripts/Audio/OnlineSessionSfxPlayer.cs"
tags: [clase, modulo/audio]
---
# OnlineSessionSfxPlayer

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/OnlineSessionSfxPlayer.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineSessionSfxPlayer {
    <<MonoBehaviour>>
    -UiSfxLibrary _library
    -OnlineSessionChannel _session
    -OnlineLobbyChannel _lobby
    -bool _playRemotePicks
    -AudioSource[] _voices
  }
  class IRandomSource:::external
  <<interface>> IRandomSource
  class OnlineLobbyChannel:::external
  <<ScriptableObject>> OnlineLobbyChannel
  class OnlineLobbySnapshot:::external
  <<struct>> OnlineLobbySnapshot
  class OnlineSessionChannel:::external
  <<ScriptableObject>> OnlineSessionChannel
  class SfxVoicePool:::external
  class UiSfxLibrary:::external
  <<ScriptableObject>> UiSfxLibrary
  OnlineSessionSfxPlayer --> IRandomSource
  OnlineSessionSfxPlayer --> OnlineLobbyChannel
  OnlineSessionSfxPlayer --> OnlineLobbySnapshot
  OnlineSessionSfxPlayer --> OnlineSessionChannel
  OnlineSessionSfxPlayer --> SfxVoicePool
  OnlineSessionSfxPlayer --> UiSfxLibrary
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IRandomSource]], [[OnlineLobbyChannel]], [[OnlineLobbyCueRules]], [[OnlineLobbyPhase]], [[OnlineLobbySnapshot]], [[OnlineSessionChannel]], [[OnlineSessionCueRules]], [[OnlineSessionStatus]], [[Phase]], [[SfxVoicePool]], [[Status]], [[SystemRandomSource]], [[UiSfxCue]], [[UiSfxLibrary]]
