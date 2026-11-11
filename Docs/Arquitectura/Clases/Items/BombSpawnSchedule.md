---
modulo: Items
namespace: AlienFarms.Items
tipo: class
archivo: "Assets/Scripts/Items/BombSpawnSchedule.cs"
tags: [clase, modulo/items]
---
# BombSpawnSchedule

> Pure spawn timer for bombs. Counts down only while running and while there is a free slot (the timer pauses while the max number of bombs is in play). Once due, it stays due until the caller reports the outcome with `OnSpawned` or `OnSpawnFailed`.

**Módulo:** [[Modulo Items|Items]] · **Namespace:** `AlienFarms.Items` · **Archivo:** `Assets/Scripts/Items/BombSpawnSchedule.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombSpawnSchedule {
    +bool IsRunning
    +float TimeUntilNext
    +Start(float initialDelay) void
    +Stop() void
    +Tick(float deltaTime, int activeCount, int maxActive) bool
    +OnSpawned(float nextInterval) void
    +OnSpawnFailed(float retryDelay) void
    +NextInterval(Vector2 range, IRandomSource rng)$ float
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IRandomSource]]

## Usado por
[[BombSpawner]]
