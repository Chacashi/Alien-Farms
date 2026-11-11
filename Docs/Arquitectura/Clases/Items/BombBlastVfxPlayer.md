---
modulo: Items
namespace: AlienFarms.Items
tipo: MonoBehaviour
archivo: "Assets/Scripts/Items/BombBlastVfxPlayer.cs"
tags: [clase, modulo/items]
---
# BombBlastVfxPlayer

> Plays a pooled `BombBlastVfx` on every `GameEvents.OnBombDetonated`.

**Módulo:** [[Modulo Items|Items]] · **Namespace:** `AlienFarms.Items` · **Archivo:** `Assets/Scripts/Items/BombBlastVfxPlayer.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombBlastVfxPlayer {
    <<MonoBehaviour>>
    -BombData _data
    -Transform _container
  }
  class BombBlastVfx:::external
  <<MonoBehaviour>> BombBlastVfx
  class BombData:::external
  <<ScriptableObject>> BombData
  BombBlastVfxPlayer --> BombBlastVfx
  BombBlastVfxPlayer --> BombData
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[BombBlastVfx]], [[BombData]], [[GameEvents]], [[PlayerId]]

## Eventos que escucha
[[Eventos GameEvents#OnBombDetonated|OnBombDetonated]]
