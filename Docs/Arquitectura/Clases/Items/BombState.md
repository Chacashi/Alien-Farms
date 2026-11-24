---
modulo: Items
namespace: AlienFarms.Items
tipo: enumeration
archivo: "Assets/Scripts/Items/BombState.cs"
tags: [clase, modulo/items]
---
# BombState

> Lifecycle of a pooled `Bomb`.

**Módulo:** [[Modulo Items|Items]] · **Namespace:** `AlienFarms.Items` · **Archivo:** `Assets/Scripts/Items/BombState.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombState {
    <<enumeration>>
    Inactive
    Idle
    Carried
    Armed
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Inactive`, `Idle`, `Carried`, `Armed`

## Usado por
[[Bomb]], [[BombBlastRules]], [[BombNetState]], [[BombSpawner]], [[NetworkBomb]]
