---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: static
archivo: "Assets/Scripts/Networking/Match/MatchClock.cs"
tags: [clase, modulo/networking]
---
# MatchClock

> What a client shows from the replicated server times (0 messages per second for the timer): the whole seconds left of the countdown and of the round, rounded up like MatchTimer.DisplaySeconds.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/MatchClock.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchClock {
    <<static>>
    +CountdownSecondsLeft(double countdownEndTime, double now)$ int
    +DisplaySeconds(MatchPhase phase, double roundEndTime, double now, float fullDuration)$ int
    +DisplaySeconds(MatchPhase phase, double roundEndTime, double now, float fullDuration, float frozenRemaining)$ int
    +TicksCountdown(MatchPhase phase, float frozenRemaining)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MatchPhase]]

## Usado por
[[NetworkMatchState]]
