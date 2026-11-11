---
modulo: Menu
namespace: AlienFarms.Menu
tipo: MonoBehaviour
archivo: "Assets/Scripts/Menu/MenuLobby.cs"
tags: [clase, modulo/menu]
---
# MenuLobby

> MainMenu player registry: listens to `PlayerInputManager` (MenuPlayer prefab), assigns each joined player its `PlayerId` from the player index and keeps its `PlayerMenuInput` and `PlayerInput` (devices and control scheme for the match session).

**Módulo:** [[Modulo Menu|Menu]] · **Namespace:** `AlienFarms.Menu` · **Archivo:** `Assets/Scripts/Menu/MenuLobby.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
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
  class PlayerMenuInput:::external
  <<MonoBehaviour>> PlayerMenuInput
  MenuLobby --> PlayerMenuInput
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]], [[PlayerIdentity]], [[PlayerMenuInput]]

## Usado por
[[MenuFlowController]]
