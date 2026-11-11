---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/CharacterDefinition.cs"
tags: [clase, modulo/data]
---
# CharacterDefinition

> A playable character: display name, optional portrait and color theme. The 3D visual is a pre-existing child of the Player prefab matched by this asset (see PlayerAppearance), so no prefab is referenced here.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/CharacterDefinition.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterDefinition {
    <<ScriptableObject>>
    -string _displayName
    -Sprite _portrait
    -CharacterTheme _theme
    +string DisplayName
    +Sprite Portrait
    +CharacterTheme Theme
  }
  class CharacterTheme:::external
  <<struct>> CharacterTheme
  CharacterDefinition --> CharacterTheme
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterTheme]]

## Usado por
[[CharacterCardView]], [[CharacterRoster]], [[CharacterSelectView]], [[CharacterVisual]], [[GameEvents]], [[MatchResultsView]], [[MatchSession]], [[MenuFlowController]], [[NetworkPlayer]], [[NetworkPlayerRoster]], [[PauseMenuView]], [[PlayerAppearance]], [[PlayerCharacterCache]], [[PlayerHudThemeView]], [[PlayerJoinCoordinator]], [[PlayerRig]], [[PlayerSlotStatusView]], [[SessionSlot]]
