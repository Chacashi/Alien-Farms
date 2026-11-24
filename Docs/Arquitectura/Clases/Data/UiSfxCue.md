---
modulo: Data
namespace: AlienFarms.Data
tipo: enumeration
archivo: "Assets/Scripts/Data/UiSfxCue.cs"
tags: [clase, modulo/data]
---
# UiSfxCue

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/UiSfxCue.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class UiSfxCue {
    <<enumeration>>
    None
    CreateRoom
    JoinRoom
    CharacterConfirm
    CharacterRelease
    PlayerJoined
    ConnectionSucceeded
    ConnectionLost
    MatchSuspended
    PlayerReconnected
    MatchResumed
    MatchPaused
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`None`, `CreateRoom`, `JoinRoom`, `CharacterConfirm`, `CharacterRelease`, `PlayerJoined`, `ConnectionSucceeded`, `ConnectionLost`, `MatchSuspended`, `PlayerReconnected`, `MatchResumed`, `MatchPaused`

## Usado por
[[CharacterSelectSfxPlayer]], [[MatchConnectionCueTracker]], [[MatchStatusSfxPlayer]], [[OnlineLobbyCueRules]], [[OnlineSessionCueRules]], [[OnlineSessionSfxPlayer]], [[UiSfxLibrary]]
