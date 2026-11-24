---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: class
archivo: "Assets/Scripts/Networking/Persistence/MatchStatsTally.cs"
tags: [clase, modulo/networking]
---
# MatchStatsTally

> Decides which round this instance records (plain C#): only one whose Playing phase it saw live, once. A reconnection that enters straight into Results (the replica re-raises the result) never counts again. Countdown and WaitingForPlayers start over.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/MatchStatsTally.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchStatsTally {
    +bool SawPlayingLive
    +bool IsCompleted
    +bool IsRoundInProgress
    +OnPhaseChanged(MatchPhase previous, MatchPhase current) void
    +TryComplete(PlayerId local, MatchResult result, int animals, int tackles, PlayerStatsSummary delta) bool
    +TryCompleteEarly(bool localWon, int animals, int tackles, PlayerStatsSummary delta) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchPhase]], [[MatchResult]], [[PlayerId]], [[PlayerStatsRules]], [[PlayerStatsSummary]]

## Usado por
[[OnlineMatchStatsRecorder]]
