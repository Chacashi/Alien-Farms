---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/PlayerSettings.cs"
tags: [clase, modulo/data]
---
# PlayerSettings

> Tunable movement, camera, grab, aim and tackle values shared by player characters.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/PlayerSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerSettings {
    <<ScriptableObject>>
    -float _walkSpeed
    -float _sprintSpeed
    -float _acceleration
    -float _deceleration
    -float _rotationSpeed
    -float _mouseSensitivity
    -Vector2 _stickSensitivity
    -bool _invertY
    -float _minPitch
    -float _maxPitch
    -float _defaultPitch
    -float _distance
    -float _collisionRadius
    ...47 más
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PlayerGrabber]], [[PlayerMovement]], [[PlayerStunReceiver]], [[PlayerTackle]], [[ThirdPersonCamera]]
