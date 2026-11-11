---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/ResultsMenuController.cs"
tags: [clase, modulo/ui]
---
# ResultsMenuController

> Gives the results menu to the winner (Player 1 on a draw) once the match accepts a rematch (`MatchManager.IsRematchReady`, the same clock that gates the rematch), so a button held at the end of the round does not click it. Handed to the other player if the owner loses its device; released when leaving Results.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/ResultsMenuController.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ResultsMenuController {
    <<MonoBehaviour>>
    -MatchManager _match
    -PlayerMenuRouter _menus
    -GameObject _menuRoot
    -GameObject _firstSelected
  }
  class MatchManager:::external
  <<MonoBehaviour>> MatchManager
  class PlayerMenuRouter:::external
  <<MonoBehaviour>> PlayerMenuRouter
  ResultsMenuController --> MatchManager
  ResultsMenuController --> PlayerMenuRouter
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[MatchManager]], [[MatchPhase]], [[MatchResult]], [[PlayerId]], [[PlayerMenuRouter]]

## Eventos que escucha
[[Eventos GameEvents#OnMatchEnded|OnMatchEnded]], [[Eventos GameEvents#OnMatchPhaseChanged|OnMatchPhaseChanged]], [[Eventos GameEvents#OnPlayerDeviceLost|OnPlayerDeviceLost]], [[Eventos GameEvents#OnPlayerDeviceRegained|OnPlayerDeviceRegained]]
