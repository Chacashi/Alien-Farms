---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/MatchResultsView.cs"
tags: [clase, modulo/ui]
---
# MatchResultsView

> Shows the round result (winner by character name, both names and the score) while the match is in Results. The winner text is tinted with the winner's Theme.Secondary; names use the dark Theme.Accent (readable on the cream wood panel) and optional stripes under them carry Theme.Secondary. Lives on an always-active object and toggles `_panelRoot`. Buttons (rematch, title) are wired in the scene; the menu owner is set by ResultsMenuController.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/MatchResultsView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchResultsView {
    <<MonoBehaviour>>
    -LocalizedString _winnerEntry
    -LocalizedString _drawEntry
    -string _scoreFormat
    -LocalizedString _player1Label
    -LocalizedString _player2Label
    -GameObject _panelRoot
    -TMP_Text _winnerText
    -TMP_Text _scoreText
    -TMP_Text _nameP1
    -TMP_Text _nameP2
    -Image _nameStripeP1
    -Image _nameStripeP2
  }
  class LocalizedStringBinding:::external
  class MatchResult:::external
  <<struct>> MatchResult
  class PlayerCharacterCache:::external
  MatchResultsView --> LocalizedStringBinding
  MatchResultsView --> MatchResult
  MatchResultsView --> PlayerCharacterCache
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[GameEvents]], [[LocalizationKeys]], [[LocalizedStringBinding]], [[MatchPhase]], [[MatchResult]], [[PlayerCharacterCache]], [[PlayerId]]

## Eventos que escucha
[[Eventos GameEvents#OnMatchEnded|OnMatchEnded]], [[Eventos GameEvents#OnMatchPhaseChanged|OnMatchPhaseChanged]], [[Eventos GameEvents#OnPlayerCharacterAssigned|OnPlayerCharacterAssigned]]
