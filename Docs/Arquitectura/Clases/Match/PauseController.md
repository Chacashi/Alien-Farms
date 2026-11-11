---
modulo: Match
namespace: AlienFarms.Match
tipo: MonoBehaviour
archivo: "Assets/Scripts/Match/PauseController.cs"
tags: [clase, modulo/match]
---
# PauseController

> Game pause: freezes time (`Time.timeScale` = 0) and audio, and routes the pause menu to the player who requested it; the other player is blocked. Only the pausing player navigates. Methods are wired to menu buttons.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/PauseController.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
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
  class MatchExit:::external
  <<MonoBehaviour>> MatchExit
  class MatchManager:::external
  <<MonoBehaviour>> MatchManager
  class PlayerMenuRouter:::external
  <<MonoBehaviour>> PlayerMenuRouter
  class SubMenuPanel:::external
  <<MonoBehaviour>> SubMenuPanel
  PauseController --> MatchExit
  PauseController --> MatchManager
  PauseController --> PlayerMenuRouter
  PauseController --> SubMenuPanel
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[MatchExit]], [[MatchManager]], [[PauseRules]], [[PlayerId]], [[PlayerMenuRouter]], [[SubMenuPanel]]

## Usado por
[[GameModeRouter]]

## Eventos que publica
[[Eventos GameEvents#OnGamePaused|OnGamePaused]], [[Eventos GameEvents#OnGameResumed|OnGameResumed]]

## Eventos que escucha
[[Eventos GameEvents#OnMenuCancelPressed|OnMenuCancelPressed]], [[Eventos GameEvents#OnPauseRequested|OnPauseRequested]], [[Eventos GameEvents#OnPlayerDeviceLost|OnPlayerDeviceLost]], [[Eventos GameEvents#OnPlayerDeviceRegained|OnPlayerDeviceRegained]], [[Eventos GameEvents#OnPlayerLeft|OnPlayerLeft]]
