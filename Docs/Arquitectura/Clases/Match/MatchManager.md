---
modulo: Match
namespace: AlienFarms.Match
tipo: MonoBehaviour
archivo: "Assets/Scripts/Match/MatchManager.cs"
tags: [clase, modulo/match]
---
# MatchManager

> Scene composition root of the match: owns the phase state machine, score and timer, and bridges them to the players (via `PlayerJoinCoordinator`) and the animals (via `AnimalSpawner`). The score (sum of each captured animal's points) mirrors zone occupancy from `GameEvents.OnAnimalCaptured` / `GameEvents.OnAnimalLeftZone` while playing; outside Playing it is frozen (`CaptureRules.TracksScore`), so the result published on entering Results is also the final `GetScore` and what the online score board replicates. The players come from an `IPlayerRoster`: the serialized coordinator unless another one is bound with `Bind` before Start.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/MatchManager.cs`

## Hereda / implementa
`MonoBehaviour`, [[IMatchContext]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchManager {
    <<MonoBehaviour>>
    -MatchSettings _settings
    -PlayerJoinCoordinator _players
    -AnimalSpawner _spawner
    -ObstacleField _obstacles
    -BombSpawner _bombs
    +MatchPhase CurrentPhase
    +float RemainingTime
    +bool IsSuspended
    +bool IsResultsInputReady
    +bool IsRematchReady
    +MatchSettings Settings
    +bool ForfeitOnPlayerMissing
    +PlayerId ForfeitWinner
    ...11 más
  }
  class AnimalSpawner:::external
  <<MonoBehaviour>> AnimalSpawner
  class BombSpawner:::external
  <<MonoBehaviour>> BombSpawner
  class IMatchContext:::external
  <<interface>> IMatchContext
  class IPlayerRoster:::external
  <<interface>> IPlayerRoster
  class MatchScore:::external
  class MatchSettings:::external
  <<ScriptableObject>> MatchSettings
  class MatchStateMachine:::external
  class MatchTimer:::external
  class ObstacleField:::external
  <<MonoBehaviour>> ObstacleField
  class PlayerJoinCoordinator:::external
  <<MonoBehaviour>> PlayerJoinCoordinator
  MatchManager --> AnimalSpawner
  MatchManager --> BombSpawner
  MatchManager --> IPlayerRoster
  MatchManager --> MatchScore
  MatchManager --> MatchSettings
  MatchManager --> MatchStateMachine
  MatchManager --> MatchTimer
  MatchManager --> ObstacleField
  MatchManager --> PlayerJoinCoordinator
  MatchManager ..|> IMatchContext
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[AnimalData]], [[AnimalSpawner]], [[BombSpawner]], [[CaptureRules]], [[GameEvents]], [[IMatchContext]], [[IPlayerRoster]], [[MatchPhase]], [[MatchResumeAction]], [[MatchScore]], [[MatchSettings]], [[MatchStateMachine]], [[MatchSuspendRules]], [[MatchTimer]], [[ObstacleField]], [[PlayerId]], [[PlayerJoinCoordinator]]

## Usado por
[[GameModeRouter]], [[LocalMatchExitHandler]], [[MatchExit]], [[NetworkMatchExit]], [[NetworkMatchState]], [[OnlineMatchBootstrap]], [[PauseController]], [[ReconnectGraceCoordinator]], [[ResultsMenuController]]

## Eventos que publica
[[Eventos GameEvents#OnMatchTimeChanged|OnMatchTimeChanged]], [[Eventos GameEvents#OnPenInventoryChanged|OnPenInventoryChanged]], [[Eventos GameEvents#OnScoreChanged|OnScoreChanged]]

## Eventos que escucha
[[Eventos GameEvents#OnAnimalCaptured|OnAnimalCaptured]], [[Eventos GameEvents#OnAnimalLeftZone|OnAnimalLeftZone]]
