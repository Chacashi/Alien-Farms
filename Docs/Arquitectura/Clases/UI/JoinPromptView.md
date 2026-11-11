---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/JoinPromptView.cs"
tags: [clase, modulo/ui]
---
# JoinPromptView

> Shows the "press a button to join" prompt while the lobby is not full. When a MainMenu session is ready the players join by themselves at Start, so the prompt stays hidden for the first frame and only shows afterwards if a session player could not be joined.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/JoinPromptView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class JoinPromptView {
    <<MonoBehaviour>>
    -GameObject _promptRoot
    -int _maxPlayers
    -MatchSession _session
  }
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  JoinPromptView --> MatchSession
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[MatchSession]], [[PlayerId]]

## Eventos que escucha
[[Eventos GameEvents#OnPlayerJoined|OnPlayerJoined]], [[Eventos GameEvents#OnPlayerLeft|OnPlayerLeft]]
