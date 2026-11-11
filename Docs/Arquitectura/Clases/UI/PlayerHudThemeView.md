---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/PlayerHudThemeView.cs"
tags: [clase, modulo/ui]
---
# PlayerHudThemeView

> Styles one player's HUD block with its character: name, portrait and theme colors. The score number itself is written by `ScoreView`; this view only tints it. Applied once per character assignment.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/PlayerHudThemeView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerHudThemeView {
    <<MonoBehaviour>>
    -PlayerId _player
    -CharacterDefinition _default
    -TMP_Text _name
    -Image _portrait
    -Image _themeStripe
    -TMP_Text _score
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  PlayerHudThemeView --> CharacterDefinition
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[CharacterTheme]], [[GameEvents]], [[PlayerId]]

## Eventos que escucha
[[Eventos GameEvents#OnPlayerCharacterAssigned|OnPlayerCharacterAssigned]], [[Eventos GameEvents#OnPlayerLeft|OnPlayerLeft]]
