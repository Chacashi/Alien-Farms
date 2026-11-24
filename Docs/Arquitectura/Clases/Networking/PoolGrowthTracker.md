---
modulo: Networking
namespace: AlienFarms.Networking.Pooling
tipo: class
archivo: "Assets/Scripts/Networking/Pooling/PoolGrowthTracker.cs"
tags: [clase, modulo/networking]
---
# PoolGrowthTracker

> Pure: how many instances a pool created beyond everything seen so far. Starts at the prewarm size, so instances created at load time (even when a replicated spawn arrives before the prewarm) never count as growth, while the first spawn that instantiates past it does.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Pooling` · **Archivo:** `Assets/Scripts/Networking/Pooling/PoolGrowthTracker.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PoolGrowthTracker {
    +int Reference
    +Observe(int created) int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PoolLifecycleRules]]

## Usado por
[[NetworkPoolLog]]
