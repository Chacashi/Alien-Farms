---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Match/OnlineMatchBootstrap.cs"
tags: [clase, modulo/networking]
---
# OnlineMatchBootstrap

> Online setup of the shared match objects. Pure client: the match is the server's, so MatchManager is disabled and the spawners and obstacle field become Replicas (state arrives from the network). On the server everything stays Authority. Every instance: the match exit buttons use the `OnlineMatchExitHandler`. Pure client: when this client is dropped (host left, kicked) the NetworkRoot is torn down and the main menu loaded on the next frame, leaving the reason on the anchor for the menu. Lives under OnlineModeRoot: the GameModeRouter activates it inside its own Awake, so this Awake runs before the obstacle field's Awake and the spawners' Start.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/OnlineMatchBootstrap.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineMatchBootstrap {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -MatchSession _session
    -MatchManager _match
    -MatchExit _exit
    -AnimalSpawner _animalSpawner
    -BombSpawner _bombSpawner
    -ObstacleField _obstacles
    +SimulationRole AppliedRole
  }
  class AnimalSpawner:::external
  <<MonoBehaviour>> AnimalSpawner
  class BombSpawner:::external
  <<MonoBehaviour>> BombSpawner
  class MatchExit:::external
  <<MonoBehaviour>> MatchExit
  class MatchManager:::external
  <<MonoBehaviour>> MatchManager
  class MatchSession:::external
  <<ScriptableObject>> MatchSession
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  class ObstacleField:::external
  <<MonoBehaviour>> ObstacleField
  OnlineMatchBootstrap --> AnimalSpawner
  OnlineMatchBootstrap --> BombSpawner
  OnlineMatchBootstrap --> MatchExit
  OnlineMatchBootstrap --> MatchManager
  OnlineMatchBootstrap --> MatchSession
  OnlineMatchBootstrap --> NetworkRuntimeAnchor
  OnlineMatchBootstrap --> ObstacleField
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AnimalSpawner]], [[BombSpawner]], [[DisconnectReasons]], [[MatchExit]], [[MatchManager]], [[MatchSession]], [[NetworkRootLifetime]], [[NetworkRuntimeAnchor]], [[ObstacleField]], [[OnlineMatchExitHandler]], [[SceneFlow]], [[SimulationRole]]
