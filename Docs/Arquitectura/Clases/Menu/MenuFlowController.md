---
modulo: Menu
namespace: AlienFarms.Menu
tipo: MonoBehaviour
archivo: "Assets/Scripts/Menu/MenuFlowController.cs"
tags: [clase, modulo/menu]
---
# MenuFlowController

> MainMenu composition root: Title or SubMenu -> CharacterSelect -> Loading. On the title the first joined player owns the buttons (the other is blocked); on character select every player moves its own cursor, confirms (locks) and cancels. When both have confirmed, their devices, scheme and character go to the `MatchSession` and the gameplay scene is loaded. Input state changes are always applied outside input callbacks (next frame on join, or from PlayerMenuInput's Update dispatch). Title sub-menus (`SubMenuPanel`) are navigated by the title owner; Cancel or the panel's Back button returns to the title. Online play hands the title owner to the `OnlineMenuController` (Online phase); there the match scene is loaded by the network, never here.

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
    -TitleIntroDirector _intro
    -CharacterSelectView _select
    -GameObject _loadingOverlay
    -OnlineMenuController _online
    -OnlineSessionChannel _onlineSession
    -GameObject _playOnlineButton
    +MenuPhase Phase
    +Play() void
    ...4 más
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
  class OnlineMenuController:::external
  <<MonoBehaviour>> OnlineMenuController
  class OnlineSessionChannel:::external
  <<ScriptableObject>> OnlineSessionChannel
  class PlayerMenuInput:::external
  <<MonoBehaviour>> PlayerMenuInput
  class SubMenuPanel:::external
  <<MonoBehaviour>> SubMenuPanel
  class TitleIntroDirector:::external
  <<MonoBehaviour>> TitleIntroDirector
  class TitleScreenView:::external
  <<MonoBehaviour>> TitleScreenView
  MenuFlowController --> CharacterRoster
  MenuFlowController --> CharacterSelectView
  MenuFlowController --> CharacterSelection
  MenuFlowController --> MatchSession
  MenuFlowController --> MenuLobby
  MenuFlowController --> MenuSettings
  MenuFlowController --> OnlineMenuController
  MenuFlowController --> OnlineSessionChannel
  MenuFlowController --> PlayerMenuInput
  MenuFlowController --> SubMenuPanel
  MenuFlowController --> TitleIntroDirector
  MenuFlowController --> TitleScreenView
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[CharacterRoster]], [[CharacterSelectView]], [[CharacterSelection]], [[GameEvents]], [[MatchSession]], [[MenuLobby]], [[MenuPhase]], [[MenuSettings]], [[OnlineMenuController]], [[OnlineMenuRules]], [[OnlineSessionChannel]], [[OnlineSessionStatus]], [[Phase]], [[PlayerId]], [[PlayerMenuInput]], [[PlayerSlotState]], [[SceneFlow]], [[SessionSlot]], [[Status]], [[SubMenuPanel]], [[TitleIntroDirector]], [[TitleScreenView]]

## Eventos que publica
[[Eventos GameEvents#OnCharacterPickConfirmed|OnCharacterPickConfirmed]], [[Eventos GameEvents#OnCharacterPickReleased|OnCharacterPickReleased]]
