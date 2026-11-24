---
tags: [modulo, modulo/match]
---
# Módulo Match

Carpeta `Assets/Scripts/Match/` · 20 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class CaptureRules {
    <<static>>
    +CountsForZone(AnimalState state, PlayerId zoneOwner)$ bool
    +CountsForZone(AnimalState state, PlayerId zoneOwner, MatchPhase phase)$ bool
    +TracksScore(MatchPhase phase)$ bool
    +PublishesScore(MatchPhase phase)$ bool
  }
  class CountdownState {
    +MatchPhase Phase
    +Enter() void
    +Tick(float deltaTime) void
  }
  class IMatchContext {
    <<interface>>
    +MatchSettings Settings
    +int JoinedPlayerCount
    +MatchTimer Timer
    +MatchScore Score
    +bool ForfeitOnPlayerMissing
    +PlayerId ForfeitWinner
    +IsPlayerPresent(PlayerId id) bool
    +EndByForfeit(PlayerId winner) void
    +ChangeState(MatchPhase next) void
    +ResetRound(bool respawnPlayers) void
    +SetPlayerControlsEnabled(bool enabled) void
  }
  class IMatchExitHandler {
    <<interface>>
    +RequestRematch() void
    +ReturnToTitle() bool
  }
  class LocalMatchExitHandler {
    +RequestRematch() void
    +ReturnToTitle() bool
  }
  class MatchExit {
    <<MonoBehaviour>>
    -MatchManager _match
    -MatchSession _session
    +SetHandler(IMatchExitHandler handler) void
    +Rematch() void
    +ReturnToTitle() void
  }
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
  class MatchResultRules {
    <<static>>
    +Resolve(MatchResult scored, PlayerId forfeitWinner)$ MatchResult
    +TryGetSoleRemaining(bool player1Present, bool player2Present, PlayerId remaining)$ bool
  }
  class MatchResumeAction {
    <<enumeration>>
    None
    EnableControls
    RestartCountdown
  }
  class MatchScore {
    +Get(PlayerId id) int
    +GetAnimalCount(PlayerId id) int
    +GetCountByType(PlayerId id, AnimalData type) int
    +TryAdd(PlayerId zoneOwner, object animal, int points, PlayerId movedFrom) bool
    +TryAdd(PlayerId zoneOwner, object animal, int points, object typeKey, PlayerId movedFrom) bool
    +TryCapture(MatchPhase phase, AnimalState state, PlayerId zoneOwner, object animal, int points, object typeKey, PlayerId movedFrom) bool
    +TryRelease(MatchPhase phase, PlayerId zoneOwner, object animal) bool
    +TryRemove(PlayerId zoneOwner, object animal) bool
    +Reset() void
    +Evaluate() MatchResult
  }
  class MatchState {
    <<abstract>>
    +MatchPhase Phase*
    #IMatchContext Context
    +bool IsRematchReady
    +bool IsResultsInputReady
    +Enter() void
    +Tick(float deltaTime) void
    +Exit() void
    +OnConfirmPressed(PlayerId playerId) void
    #AbortIfPlayerMissing() bool
  }
  class MatchStateMachine {
    +MatchPhase CurrentPhase
    +bool IsRematchReady
    +bool IsResultsInputReady
    +ChangeState(MatchPhase next) bool
    +Tick(float deltaTime) void
    +OnConfirmPressed(PlayerId playerId) void
  }
  class MatchSuspendRules {
    <<static>>
    +SuspendsIn(MatchPhase phase)$ bool
    +AllowsRematch(bool suspended, bool stateReady)$ bool
    +OnResume(MatchPhase phase, bool playersComplete)$ MatchResumeAction
  }
  class MatchTimer {
    +float Remaining
    +bool IsFinished
    +int DisplaySeconds
    +Reset(float duration) void
    +Tick(float deltaTime) bool
  }
  class PauseController {
    <<MonoBehaviour>>
    -MatchManager _match
    -PlayerMenuRouter _menus
    -MatchExit _exit
    -GameObject _menuRoot
    -GameObject _firstSelected
    -GameObject _mainContent
    +bool IsPaused
    +bool FreezesTime
    +PlayerId Owner
    +Pause(PlayerId owner) void
    +SetFreezesTime(bool freezesTime) void
    +Resume() void
    +RestartMatch() void
    ...3 más
  }
  class PauseRules {
    <<static>>
    +CanPause(MatchPhase phase)$ bool
  }
  class PlayingState {
    +MatchPhase Phase
    +Enter() void
    +Tick(float deltaTime) void
  }
  class ResultsState {
    +MatchPhase Phase
    +bool IsRematchReady
    +bool IsResultsInputReady
    +Enter() void
    +Tick(float deltaTime) void
    +OnConfirmPressed(PlayerId playerId) void
  }
  class TimeFormat {
    <<static>>
    +WriteMinutesSeconds(int totalSeconds, char buffer)$ int
  }
  class WaitingForPlayersState {
    +MatchPhase Phase
    +Enter() void
    +Tick(float deltaTime) void
  }
  class AnimalSpawner:::external
  <<MonoBehaviour>> AnimalSpawner
  class BombSpawner:::external
  <<MonoBehaviour>> BombSpawner
  class IPlayerRoster:::external
  <<interface>> IPlayerRoster
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  class MatchSettings:::external
  <<ScriptableObject>> MatchSettings
  class ObstacleField:::external
  <<MonoBehaviour>> ObstacleField
  class PlayerJoinCoordinator:::external
  <<MonoBehaviour>> PlayerJoinCoordinator
  class PlayerMenuRouter:::external
  <<MonoBehaviour>> PlayerMenuRouter
  class SubMenuPanel:::external
  <<MonoBehaviour>> SubMenuPanel
  CountdownState --|> MatchState
  IMatchContext --> MatchScore
  IMatchContext --> MatchSettings
  IMatchContext --> MatchTimer
  LocalMatchExitHandler --> MatchManager
  LocalMatchExitHandler --> MatchSession
  LocalMatchExitHandler ..|> IMatchExitHandler
  MatchExit --> IMatchExitHandler
  MatchExit --> MatchManager
  MatchExit --> MatchSession
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
  MatchState --> IMatchContext
  MatchStateMachine --> IMatchContext
  MatchStateMachine --> MatchState
  PauseController --> MatchExit
  PauseController --> MatchManager
  PauseController --> PlayerMenuRouter
  PauseController --> SubMenuPanel
  PlayingState --|> MatchState
  ResultsState --|> MatchState
  WaitingForPlayersState --|> MatchState
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[CaptureRules]] | static | Pure scoring rules. The score is zone occupancy: an animal counts for a zone's owner while it is captured there, whoever released it. Carrie… |
| [[CountdownState]] | class | Resets the round, freezes players and counts down before play starts. |
| [[IMatchContext]] | interface | What match states may read and do. Implemented by MatchManager (and by test fakes). |
| [[IMatchExitHandler]] | interface | What the match exit buttons do. Local: `LocalMatchExitHandler`; online, a network handler. |
| [[LocalMatchExitHandler]] | class | Local split-screen exit: rematch through the `MatchManager`; returning to the title clears the `MatchSession` and loads the main menu (time … |
| [[MatchExit]] | MonoBehaviour | Exit options of a finished (or paused) match, wired to menu buttons: rematch or back to the title screen. What they do is delegated to an `I… |
| [[MatchManager]] | MonoBehaviour | Scene composition root of the match: owns the phase state machine, score and timer, and bridges them to the players (via `PlayerJoinCoordina… |
| [[MatchResultRules]] | static | How the published `MatchResult` is built from the scored one. Shared by the match (server / local) and the online replica, so both raise the… |
| [[MatchResumeAction]] | enumeration | What the match does when a suspension ends (see `MatchSuspendRules.OnResume`). |
| [[MatchScore]] | class | Zone occupancy for the current round: the animals each player's zone holds, the points each one is worth and its type. A player's score is t… |
| [[MatchState]] | abstract | Base of the match State pattern. Plain C# (testable in EditMode). Rule: no state calls `IMatchContext.ChangeState` from `Enter`; transitions… |
| [[MatchStateMachine]] | class | Plain C# match state machine: owns one state per `MatchPhase` and performs transitions. Side effects (players, animals, score, timer) live i… |
| [[MatchSuspendRules]] | static | Pure rules of a match suspension (online reconnection grace): which phases are suspended and what to do when the suspension ends. |
| [[MatchTimer]] | class | Countdown of the round time. Driven externally with `Tick` (no Time.* access). |
| [[PauseController]] | MonoBehaviour | Game pause: freezes time (`Time.timeScale` = 0) and audio, and routes the pause menu to the player who requested it; the other player is blo… |
| [[PauseRules]] | static | Pure rules for when the game may be paused. |
| [[PlayingState]] | class | Round in progress: timer runs (1 Hz event) and score changes are published. A missing player either aborts the round (local) or ends it as a… |
| [[ResultsState]] | class | Round over: publishes the result, freezes players and restarts on Confirm after a short delay. A forfeit result (the rival left during Playi… |
| [[TimeFormat]] | static | Allocation-free time formatting into reusable char buffers. |
| [[WaitingForPlayersState]] | class | Free roam until both players have joined; captures are not scored (the countdown resets the round). |

## Depende de
[[Modulo Animals|Animals]] (4), [[Modulo Core|Core]] (34), [[Modulo Data|Data]] (6), [[Modulo Items|Items]] (1), [[Modulo Networking|Networking]] (6), [[Modulo Player|Player]] (3), [[Modulo UI|UI]] (1), [[Modulo Zones|Zones]] (1)