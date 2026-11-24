---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/PenInventoryView.cs"
tags: [clase, modulo/ui]
---
# PenInventoryView

> Shows one player's pen inventory (count per animal type). Driven only by `GameEvents.OnPenInventoryChanged`, raised locally/on the server by the match and re-raised on pure clients by the network ledger, so it never reads the animals physically inside the pen. On enable it catches up from `PenInventorySnapshot`.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/PenInventoryView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PenInventoryView {
    <<MonoBehaviour>>
    -PlayerId _player
    -AnimalSpawnSettings _spawnSettings
    -PenInventorySlotView[] _slots
    +PlayerId Player
    +ResetCounts() void
  }
  class AnimalSpawnSettings:::external
  <<ScriptableObject>> AnimalSpawnSettings
  class PenInventorySlotView:::external
  <<MonoBehaviour>> PenInventorySlotView
  PenInventoryView --> AnimalSpawnSettings
  PenInventoryView --> PenInventorySlotView
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AnimalSpawnEntry]], [[AnimalSpawnSettings]], [[GameEvents]], [[PenInventorySlotView]], [[PenInventorySnapshot]], [[PlayerId]]

## Eventos que escucha
[[Eventos GameEvents#OnPenInventoryChanged|OnPenInventoryChanged]]
