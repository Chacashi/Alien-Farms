---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/PlayerId.cs"
tags: [clase, modulo/core]
---
# PlayerId

> Identifies one of the two local players.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/PlayerId.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerId {
    <<enumeration>>
    None
    Player1
    Player2
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`None`, `Player1`, `Player2`

## Usado por
[[Animal]], [[AnimalSfxPlayer]], [[Bomb]], [[BombBlastRules]], [[BombBlastVfxPlayer]], [[BombSfxPlayer]], [[CaptureRules]], [[CharacterCardView]], [[CharacterSelectView]], [[CharacterSelection]], [[GameEvents]], [[IGrabbable]], [[IPlayerRoster]], [[IStunnable]], [[IZoneResolver]], [[JoinPromptView]], [[MatchManager]], [[MatchResult]], [[MatchResultsView]], [[MatchScore]], [[MatchSession]], [[MatchState]], [[MatchStateMachine]], [[MenuFlowController]], [[MenuLobby]], [[PauseController]], [[PauseHoldIndicatorView]], [[PauseMenuView]], [[PlayerCharacterCache]], [[PlayerGrabber]], [[PlayerHudThemeView]], [[PlayerIdExtensions]], [[PlayerIdentity]], [[PlayerJoinCoordinator]], [[PlayerMenuInput]], [[PlayerMenuRouter]], [[PlayerRig]], [[PlayerSlotStatusView]], [[PlayerStunReceiver]], [[PlayerTackle]], [[PlayerZone]], [[PlayerZoneRegistry]], [[ResultsMenuController]], [[ResultsState]], [[ScoreView]], [[SessionData]], [[SessionSlot]]
