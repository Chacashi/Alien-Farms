---
modulo: Core
namespace: AlienFarms.Core
tipo: interface
archivo: "Assets/Scripts/Core/IGrabbable.cs"
tags: [clase, modulo/core]
---
# IGrabbable

> Something a player can pick up, carry at a hold point and drop.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/IGrabbable.cs`

## Derivados / implementaciones
[[IThrowable]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IGrabbable {
    <<interface>>
    +bool CanBeGrabbed
    +PlayerId Holder
    +Vector3 Position
    +float CarrySpeedMultiplier
    +TryGrab(PlayerId holder, Transform holdPoint) bool
    +Release(Vector3 position, Vector3 velocity, DropReason reason) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[DropReason]], [[PlayerId]]

## Usado por
[[IThrowable]], [[PlayerAim]], [[PlayerGrabber]]
