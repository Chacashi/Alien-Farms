---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: class
archivo: "Assets/Scripts/Networking/Match/OnlineMatchExitHandler.cs"
tags: [clase, modulo/networking]
---
# OnlineMatchExitHandler

> Online match exit (version 1.5). Back to title: clears the session, tears the NetworkRoot down (shutdown + destroy; the MainMenu brings a fresh one) and loads the main menu. Rematch: only the server restarts the match; a client's request is ignored until NetworkMatchExit (1.6) forwards it.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/OnlineMatchExitHandler.cs`

## Hereda / implementa
[[IMatchExitHandler]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineMatchExitHandler {
    +RequestRematch() void
    +ReturnToTitle() bool
  }
  class IMatchExitHandler:::external
  <<interface>> IMatchExitHandler
  class MatchManager:::external
  <<MonoBehaviour>> MatchManager
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  OnlineMatchExitHandler --> MatchManager
  OnlineMatchExitHandler --> MatchSession
  OnlineMatchExitHandler --> NetworkRuntimeAnchor
  OnlineMatchExitHandler ..|> IMatchExitHandler
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IMatchExitHandler]], [[MatchManager]], [[MatchSession]], [[NetworkRootLifetime]], [[NetworkRuntimeAnchor]], [[SceneFlow]]

## Usado por
[[OnlineMatchBootstrap]]
