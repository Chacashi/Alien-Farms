---
modulo: Menu
namespace: AlienFarms.Menu
tipo: MonoBehaviour
archivo: "Assets/Scripts/Menu/OnlineMenuController.cs"
tags: [clase, modulo/menu]
---
# OnlineMenuController

> Online phase of the MainMenu (hub, join, room) for the title owner. Talks to the network only through the `OnlineSessionChannel` and `OnlineLobbyChannel`: buttons call their Request* methods and every channel change only marks the panel dirty. The panel is rebuilt from the channels in the next frame's Update (never inside a button click), so the owner's input scope changes outside the event system and nothing is redrawn per frame. A full rebuild also covers a MainMenu reloaded while in a session (R1).

**Módulo:** [[Modulo Menu|Menu]] · **Namespace:** `AlienFarms.Menu` · **Archivo:** `Assets/Scripts/Menu/OnlineMenuController.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineMenuController {
    <<MonoBehaviour>>
    -OnlineSessionChannel _session
    -OnlineLobbyChannel _lobby
    -CharacterRoster _roster
    -PlayerProgressChannel _progress
    -OnlinePanelView _view
    +event Action ExitRequested
    +bool IsActive
    +bool IsLoadingMatch
    +Enter(PlayerMenuInput owner) void
    +Exit() void
    +RefreshFocus() void
    +HandleCancel() bool
    +CreateRoom() void
    ...7 más
  }
  class CharacterRoster:::external
  <<ScriptableObject>> CharacterRoster
  class OnlineLobbyChannel:::external
  <<ScriptableObject>> OnlineLobbyChannel
  class OnlinePanelView:::external
  <<MonoBehaviour>> OnlinePanelView
  class OnlineSessionChannel:::external
  <<ScriptableObject>> OnlineSessionChannel
  class PlayerMenuInput:::external
  <<MonoBehaviour>> PlayerMenuInput
  class PlayerProgressChannel:::external
  <<ScriptableObject>> PlayerProgressChannel
  OnlineMenuController --> CharacterRoster
  OnlineMenuController --> OnlineLobbyChannel
  OnlineMenuController --> OnlinePanelView
  OnlineMenuController --> OnlineSessionChannel
  OnlineMenuController --> PlayerMenuInput
  OnlineMenuController --> PlayerProgressChannel
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[CharacterRoster]], [[DisplayNameSanitizer]], [[LocalizationKeys]], [[OnlineLobbyChannel]], [[OnlineLobbyPhase]], [[OnlineLobbySeat]], [[OnlineMenuRules]], [[OnlinePanelView]], [[OnlineParticipant]], [[OnlineScreen]], [[OnlineSessionChannel]], [[OnlineSessionErrorKeys]], [[OnlineSessionStatus]], [[OnlineSessionStatusRules]], [[Phase]], [[PlayerId]], [[PlayerMenuInput]], [[PlayerProgressChannel]], [[Status]]

## Usado por
[[MenuFlowController]]
