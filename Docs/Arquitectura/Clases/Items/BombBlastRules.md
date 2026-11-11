---
modulo: Items
namespace: AlienFarms.Items
tipo: static
archivo: "Assets/Scripts/Items/BombBlastRules.cs"
tags: [clase, modulo/items]
---
# BombBlastRules

> Pure rules of the stun bomb (who gets stunned, when it explodes). No Unity state, fully testable.

**Módulo:** [[Modulo Items|Items]] · **Namespace:** `AlienFarms.Items` · **Archivo:** `Assets/Scripts/Items/BombBlastRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombBlastRules {
    <<static>>
    +ShouldStun(PlayerId thrower, PlayerId target, float sqrDistanceToTarget, float radius)$ bool
    +ShouldDetonateOnContact(BombState state, bool isThrowerBody)$ bool
    +HasTimedOut(float armedTime, float maxArmedTime)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[BombState]], [[PlayerId]]

## Usado por
[[Bomb]]
