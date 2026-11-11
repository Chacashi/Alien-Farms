---
modulo: Animals
namespace: AlienFarms.Animals
tipo: static
archivo: "Assets/Scripts/Animals/AnimalLanding.cs"
tags: [clase, modulo/animals]
---
# AnimalLanding

> Pure rule that decides what an airborne animal does this physics step.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/AnimalLanding.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AnimalLanding {
    <<static>>
    +IsStuckOnEnvironment(bool touchingEnvironment, bool grounded, bool onPlayer, float speedSqr, float maxStuckSpeed)$ bool
    +ShouldPushOff(bool stuck, float now, float stuckSince, float maxStuckTime, int pushesUsed, int maxPushes)$ bool
    +PushOffDirection(Vector3 bodyCenter, Vector3 contactPoint, Vector3 contactNormal, Vector3 fallback)$ Vector3
    +TrackPlayerContact()$ void
    +Evaluate()$ AirborneOutcome
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AirborneOutcome]]

## Usado por
[[Animal]]
