---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/BombData.cs"
tags: [clase, modulo/data]
---
# BombData

> Definition of the stun bomb: prefab, carry/throw tuning, blast, VFX and sound effects.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/BombData.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombData {
    <<ScriptableObject>>
    -Bomb _prefab
    -ThrowSettings _throwSettings
    -float _carrySpeedMultiplier
    -Vector3 _holdOffset
    -float _grabCooldown
    -float _blastRadius
    -float _stunDuration
    -float _knockbackSpeed
    -float _maxArmedTime
    -float _armedBlinkInterval
    -bool _scaresAnimals
    -float _animalScareRadius
    -float _animalScareDuration
    ...31 más
  }
  class Bomb:::external
  <<MonoBehaviour>> Bomb
  class BombBlastVfx:::external
  <<MonoBehaviour>> BombBlastVfx
  class SoundCue:::external
  class ThrowSettings:::external
  <<ScriptableObject>> ThrowSettings
  BombData --> Bomb
  BombData --> BombBlastVfx
  BombData --> SoundCue
  BombData --> ThrowSettings
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Bomb]], [[BombBlastVfx]], [[SoundCue]], [[ThrowSettings]]

## Usado por
[[Bomb]], [[BombBlastVfxPlayer]], [[BombSfxPlayer]], [[BombSpawner]], [[NetworkSpawnRegistry]]
