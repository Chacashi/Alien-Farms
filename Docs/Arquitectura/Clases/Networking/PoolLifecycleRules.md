---
modulo: Networking
namespace: AlienFarms.Networking.Pooling
tipo: static
archivo: "Assets/Scripts/Networking/Pooling/PoolLifecycleRules.cs"
tags: [clase, modulo/networking]
---
# PoolLifecycleRules

> Pure rules of the pool lifecycle log (unit tested).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Pooling` · **Archivo:** `Assets/Scripts/Networking/Pooling/PoolLifecycleRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PoolLifecycleRules {
    <<static>>
    +DescribeUse(int use)$ string
    +Growth(int previousCreated, int currentCreated)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PoolGrowthTracker]]
