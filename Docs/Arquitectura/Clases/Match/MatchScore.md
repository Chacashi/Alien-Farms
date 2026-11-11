---
modulo: Match
namespace: AlienFarms.Match
tipo: class
archivo: "Assets/Scripts/Match/MatchScore.cs"
tags: [clase, modulo/match]
---
# MatchScore

> Zone occupancy for the current round: the animals each player's zone holds, the points each one is worth and its type. A player's score is the sum of the points in its zone. Idempotent (duplicate events are absorbed) and an animal is in at most one zone; the points recorded on entry are the ones removed on exit. Pure logic: animals and types are opaque keys.

**Módulo:** [[Modulo Match|Match]] · **Namespace:** `AlienFarms.Match` · **Archivo:** `Assets/Scripts/Match/MatchScore.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchScore {
    +Get(PlayerId id) int
    +GetAnimalCount(PlayerId id) int
    +GetCountByType(PlayerId id, AnimalData type) int
    +TryAdd(PlayerId zoneOwner, object animal, int points, PlayerId movedFrom) bool
    +TryAdd(PlayerId zoneOwner, object animal, int points, object typeKey, PlayerId movedFrom) bool
    +TryRemove(PlayerId zoneOwner, object animal) bool
    +Reset() void
    +Evaluate() MatchResult
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AnimalData]], [[MatchResult]], [[PlayerId]]

## Usado por
[[IMatchContext]], [[MatchManager]]
