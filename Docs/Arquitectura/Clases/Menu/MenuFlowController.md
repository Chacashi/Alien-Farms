---
modulo: Menu
namespace: AlienFarms.Menu
tipo: MonoBehaviour
archivo: "Assets/Scripts/Menu/MenuFlowController.cs"
tags: [clase, modulo/menu]
---
# MenuFlowController

> MainMenu composition root: Title -> CharacterSelect -> Loading. On the title the first joined player owns the buttons (the other is blocked); on character select every player moves its own cursor, confirms (locks) and cancels. When both have confirmed, their devices, scheme and character go to the `MatchSession` and the gameplay scene is loaded. Input state changes are always applied outside input callbacks (next frame on join, or from PlayerMenuInput's Update dispatch). Title sub-menus (`SubMenuPanel`) are navigated by the title owner; Cancel or the panel's Back button returns to the title.

**Módulo:** [[Modulo Menu|Menu]] · **Namespace:** `AlienFarms.Menu` · **Archivo:** `Assets/Scripts/Menu/MenuFlowController.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MenuFlowController {
    <<MonoBehaviour>>
    -CharacterRoster _roster
    -MenuSettings _settings
    -MenuLobby _lobby
    -MatchSession _session
    -TitleScreenView _title
    -CharacterSelectView _select
    -GameObject _loadingOverlay
    +MenuPhase Phase
    +Play() void
    +OpenSubMenu(SubMenuPanel panel) void
    +CloseSubMenu() void
    +Quit() void
  }
  class CharacterRoster:::external
  <<ScriptableObject>> CharacterRoster
  class CharacterSelectView:::external
  <<MonoBehaviour>> CharacterSelectView
  class CharacterSelection:::external
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  class MenuLobby:::external
  <<MonoBehaviour>> MenuLobby
  class MenuSettings:::external
  <<ScriptableObject>> MenuSettings
  class PlayerMenuInput:::external
  <<MonoBehaviour>> PlayerMenuInput
  class SubMenuPanel:::external
  <<MonoBehaviour>> SubMenuPanel
  class TitleScreenView:::external
  <<MonoBehaviour>> TitleScreenView
  MenuFlowController --> CharacterRoster
  MenuFlowController --> CharacterSelectView
  MenuFlowController --> CharacterSelection
  MenuFlowController --> MatchSession
  MenuFlowController --> MenuLobby
  MenuFlowController --> MenuSettings
  MenuFlowController --> PlayerMenuInput
  MenuFlowController --> SubMenuPanel
  MenuFlowController --> TitleScreenView
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[CharacterRoster]], [[CharacterSelectView]], [[CharacterSelection]], [[MatchSession]], [[MenuLobby]], [[MenuPhase]], [[MenuSettings]], [[PlayerId]], [[PlayerMenuInput]], [[PlayerSlotState]], [[SceneFlow]], [[SessionSlot]], [[SubMenuPanel]], [[TitleScreenView]]
