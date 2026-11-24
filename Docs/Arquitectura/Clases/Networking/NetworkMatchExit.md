---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Match/NetworkMatchExit.cs"
tags: [clase, modulo/networking]
---
# NetworkMatchExit

> Online rematch (server side). Each player's "Rematch" button sets its own ReadyForRematch (an owner-written NetworkVariable on its NetworkPlayer, see `OnlineMatchExitHandler`). On the server, once both players are ready and the results input delay has passed, the match is restarted (no scene change); owners clear their flag when the phase leaves Results. Returning to the title stays local (exit handler). Lives under OnlineModeRoot; does nothing off the server.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/NetworkMatchExit.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkMatchExit {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -MatchManager _match
    +bool BothReady
  }
  class MatchManager:::external
  <<MonoBehaviour>> MatchManager
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  NetworkMatchExit --> MatchManager
  NetworkMatchExit --> NetworkRuntimeAnchor
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchManager]], [[MatchPhase]], [[NetworkPlayer]], [[NetworkRuntimeAnchor]], [[PlayerId]]
