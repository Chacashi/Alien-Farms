---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/UiSfxLibrary.cs"
tags: [clase, modulo/data]
---
# UiSfxLibrary

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/UiSfxLibrary.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class UiSfxLibrary {
    <<ScriptableObject>>
    -SoundCue _createRoom
    -SoundCue _joinRoom
    -SoundCue _characterConfirm
    -SoundCue _characterRelease
    -SoundCue _playerJoined
    -SoundCue _connectionSucceeded
    -SoundCue _connectionLost
    -SoundCue _matchSuspended
    -SoundCue _playerReconnected
    -SoundCue _matchResumed
    -SoundCue _matchPaused
    -float _resumeAfterReconnectDelay
    -float _arrivalCueDelay
    ...3 más
  }
  class SoundCue:::external
  UiSfxLibrary --> SoundCue
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[SoundCue]], [[UiSfxCue]]

## Usado por
[[CharacterSelectSfxPlayer]], [[MatchStatusSfxPlayer]], [[OnlineSessionSfxPlayer]]
