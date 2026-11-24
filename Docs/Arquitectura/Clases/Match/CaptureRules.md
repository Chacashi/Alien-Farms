---
modulo: Match
namespace: AlienFarms.Match
tipo: static
archivo: "Assets/Scripts/Match/CaptureRules.cs"
tags: [clase, modulo/match]
---
# CaptureRules

> Pure scoring rules. The score is zone occupancy: an animal counts for a zone's owner while it is captured there, whoever released it. Carried and airborne animals never count. The round's bookkeeping (score and pen contents) only changes while playing: from the moment the phase leaves Playing it is frozen, so the published result, the replicated score board and the match's own score are the same final value.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/CaptureRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CaptureRules {
    <<static>>
    +CountsForZone(AnimalState state, PlayerId zoneOwner)$ bool
    +CountsForZone(AnimalState state, PlayerId zoneOwner, MatchPhase phase)$ bool
    +TracksScore(MatchPhase phase)$ bool
    +PublishesScore(MatchPhase phase)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AnimalState]], [[MatchPhase]], [[PlayerId]]

## Usado por
[[MatchManager]], [[MatchScore]]
