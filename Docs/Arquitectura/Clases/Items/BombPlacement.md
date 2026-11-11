---
modulo: Items
namespace: AlienFarms.Items
tipo: static
archivo: "Assets/Scripts/Items/BombPlacement.cs"
tags: [clase, modulo/items]
---
# BombPlacement

> Pure placement helper for bomb spawns.

**Módulo:** [[Modulo Items|Items]] · **Namespace:** `AlienFarms.Items` · **Archivo:** `Assets/Scripts/Items/BombPlacement.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombPlacement {
    <<static>>
    +RandomPointInArea(Bounds area, float edgeMargin, IRandomSource rng)$ Vector3
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IRandomSource]]

## Usado por
[[BombSpawner]]
