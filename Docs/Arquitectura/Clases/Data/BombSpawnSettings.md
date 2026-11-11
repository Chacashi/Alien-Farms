---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/BombSpawnSettings.cs"
tags: [clase, modulo/data]
---
# BombSpawnSettings

> When and where stun bombs appear in the Animal Zone.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/BombSpawnSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombSpawnSettings {
    <<ScriptableObject>>
    -int _maxActive
    -float _initialDelay
    -Vector2 _respawnIntervalRange
    -float _retryDelay
    -float _edgeMargin
    -float _spawnClearance
    -float _playerClearance
    -int _maxPlacementAttempts
    -float _spawnDropHeight
    -int _poolMaxSize
    -float _killHeight
    +int MaxActive
    +float InitialDelay
    ...9 más
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[BombSpawner]]
