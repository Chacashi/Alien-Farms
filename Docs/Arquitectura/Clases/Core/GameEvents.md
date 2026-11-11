---
modulo: Core
namespace: AlienFarms.Core
tipo: static
archivo: "Assets/Scripts/Core/GameEvents.cs"
tags: [clase, modulo/core]
---
# GameEvents

> Global static events for decoupled communication between systems. Every per-player payload carries the `PlayerId` involved. Exception: match phase, countdown and match time are global to the round (shared by both players), so they carry no PlayerId; neither do bomb spawn/despawn, which belong to no player (the bomb lies in the shared Animal Zone or leaves play).

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/GameEvents.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class GameEvents {
    <<static>>
    +event Action~PlayerId~ OnPlayerJoined$
    +event Action~PlayerId~ OnPlayerLeft$
    +event Action~PlayerId~ OnPauseRequested$
    +event Action~PlayerId, float~ OnPauseHoldProgress$
    +event Action~PlayerId~ OnGamePaused$
    +event Action~PlayerId~ OnGameResumed$
    +event Action~PlayerId~ OnMenuCancelPressed$
    +event Action~PlayerId~ OnPlayerDeviceLost$
    +event Action~PlayerId~ OnPlayerDeviceRegained$
    +event Action~PlayerId, CharacterDefinition~ OnPlayerCharacterAssigned$
    +event Action~PlayerId, Animal~ OnAnimalGrabbed$
    +event Action~PlayerId, Animal, DropReason~ OnAnimalDropped$
    +event Action~PlayerId, Animal~ OnAnimalCaptured$
    ...45 más
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[Bomb]], [[CharacterDefinition]], [[DropReason]], [[MatchPhase]], [[MatchResult]], [[PlayerId]]

## Usado por
[[Animal]], [[AnimalSfxPlayer]], [[Bomb]], [[BombBlastVfxPlayer]], [[BombSfxPlayer]], [[BombSpawner]], [[CountdownState]], [[CountdownView]], [[JoinPromptView]], [[MatchManager]], [[MatchResultsView]], [[MatchStateMachine]], [[MatchTimerView]], [[PauseController]], [[PauseHoldIndicatorView]], [[PauseMenuView]], [[PlayerHudThemeView]], [[PlayerJoinCoordinator]], [[PlayerMenuInput]], [[PlayerMenuRouter]], [[PlayerPauseInput]], [[PlayerStunReceiver]], [[PlayingState]], [[ResultsMenuController]], [[ResultsState]], [[ScoreView]]
