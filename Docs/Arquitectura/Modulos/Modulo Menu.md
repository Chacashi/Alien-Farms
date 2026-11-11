---
tags: [modulo, modulo/menu]
---
# Módulo Menu

Carpeta `Assets/Scripts/Menu/` · 4 tipos. Las cajas punteadas son tipos de otros módulos.

Leyenda: `--|>` hereda · `..|>` implementa · `-->` tiene un campo/propiedad de ese tipo. Se muestran campos `[SerializeField]` y la API pública.

## Diagrama de clases
```mermaid
classDiagram
  direction LR
  class CharacterSelection {
    +int CharacterCount
    +int MaxPlayers
    +int JoinedCount
    +bool AllConfirmed
    +IsJoined(PlayerId id) bool
    +IsConfirmed(PlayerId id) bool
    +GetHover(PlayerId id) int
    +GetConfirmedIndex(PlayerId id) int
    +LockedBy(int characterIndex) PlayerId
    +Join(PlayerId id) bool
    +Leave(PlayerId id) bool
    +Move(PlayerId id, int direction) bool
    +Confirm(PlayerId id) bool
    +Cancel(PlayerId id) bool
  }
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
  class MenuLobby {
    <<MonoBehaviour>>
    -PlayerInputManager _manager
    +event Action~PlayerMenuInput~ Joined
    +event Action~PlayerId~ Left
    +int JoinedCount
    +TryGet(PlayerId id, PlayerMenuInput menu) bool
    +TryGetInput(PlayerId id, PlayerInput input) bool
    +SetJoiningEnabled(bool enabled) void
  }
  class MenuPhase {
    <<enumeration>>
    Title
    CharacterSelect
    Loading
    SubMenu
  }
  class CharacterRoster:::external
  <<ScriptableObject>> CharacterRoster
  class CharacterSelectView:::external
  <<MonoBehaviour>> CharacterSelectView
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
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
  MenuLobby --> PlayerMenuInput
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Tipos

| Tipo | Clase | Descripción |
|---|---|---|
| [[CharacterSelection]] | class | Plain C# model of the character select screen: which players joined, which character each one hovers and which characters are locked (confir… |
| [[MenuFlowController]] | MonoBehaviour | MainMenu composition root: Title -> CharacterSelect -> Loading. On the title the first joined player owns the buttons (the other is blocked)… |
| [[MenuLobby]] | MonoBehaviour | MainMenu player registry: listens to `PlayerInputManager` (MenuPlayer prefab), assigns each joined player its `PlayerId` from the player ind… |
| [[MenuPhase]] | enumeration | Screens of the MainMenu scene, in flow order. |

## Depende de
[[Modulo Core|Core]] (5), [[Modulo Data|Data]] (5), [[Modulo Input|Input]] (2), [[Modulo UI|UI]] (4)