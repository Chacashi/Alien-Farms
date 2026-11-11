---
modulo: Items
namespace: AlienFarms.Items
tipo: MonoBehaviour
archivo: "Assets/Scripts/Items/BombBlastVfx.cs"
tags: [clase, modulo/items]
---
# BombBlastVfx

> Pooled blast effect: a sphere that expands to the blast size and fades out, then returns to its pool.

**Módulo:** [[Modulo Items|Items]] · **Namespace:** `AlienFarms.Items` · **Archivo:** `Assets/Scripts/Items/BombBlastVfx.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombBlastVfx {
    <<MonoBehaviour>>
    -Renderer _renderer
    +Play(Vector3 position, float radius, float duration, IObjectPool~BombBlastVfx~ pool) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[BombBlastVfxPlayer]], [[BombData]]
