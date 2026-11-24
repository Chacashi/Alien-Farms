---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Match/ReconnectGraceCoordinator.cs"
tags: [clase, modulo/networking]
---
# ReconnectGraceCoordinator

> Server-only composition of the reconnection grace in the gameplay scene (under OnlineModeRoot). While it lives the approval handler keeps a dropped player's seat (`ConnectionApprovalHandler.SetGraceEnabled`): Seat reserved: the match is suspended (only in the phases `MatchSuspendRules.SuspendsIn`) and its round clock frozen. Seat restored (its avatar spawned again), released as Left or TimedOut: once no seat awaits anybody the clock resumes and the suspension ends (the match then aborts by itself if a player is missing). A timed-out member is removed from the online session. The host leaving on purpose (`NetworkRootLifetime.HostLeaveStarted`): the match is suspended and its clock frozen for good, so nothing ends or scores while the clients get the leaving notice. After every change the seat presence board is rebuilt from the seat table and written to `NetworkSeatPresence`, the single emitter of `GameEvents.OnPlayerConnectionChanged`. Disabled off the server.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/ReconnectGraceCoordinator.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ReconnectGraceCoordinator {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    -NetworkSettings _settings
    -MatchManager _match
    -NetworkMatchState _matchState
    -NetworkSeatPresence _presence
    -NetworkPlayerRoster _roster
  }
  class ConnectionApprovalHandler:::external
  <<MonoBehaviour>> ConnectionApprovalHandler
  class MatchManager:::external
  <<MonoBehaviour>> MatchManager
  class NetworkMatchState:::external
  class NetworkPlayerRoster:::external
  <<MonoBehaviour>> NetworkPlayerRoster
  class NetworkRootLifetime:::external
  <<MonoBehaviour>> NetworkRootLifetime
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  class NetworkSeatPresence:::external
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class SeatPresenceBoard:::external
  <<struct>> SeatPresenceBoard
  ReconnectGraceCoordinator --> ConnectionApprovalHandler
  ReconnectGraceCoordinator --> MatchManager
  ReconnectGraceCoordinator --> NetworkMatchState
  ReconnectGraceCoordinator --> NetworkPlayerRoster
  ReconnectGraceCoordinator --> NetworkRootLifetime
  ReconnectGraceCoordinator --> NetworkRuntimeAnchor
  ReconnectGraceCoordinator --> NetworkSeatPresence
  ReconnectGraceCoordinator --> NetworkSettings
  ReconnectGraceCoordinator --> SeatPresenceBoard
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ConnectionApprovalHandler]], [[MatchManager]], [[MatchPhase]], [[MatchSuspendRules]], [[NetworkMatchState]], [[NetworkPlayerRoster]], [[NetworkRootLifetime]], [[NetworkRuntimeAnchor]], [[NetworkSeatPresence]], [[NetworkSettings]], [[OnlineSessionService]], [[PlayerConnectionStatus]], [[PlayerId]], [[PlayerSlot]], [[ReconnectGraceRules]], [[SeatPresenceBoard]], [[ServerSlotDirectory]]
