---
modulo: Core
namespace: AlienFarms.Core
tipo: interface
archivo: "Assets/Scripts/Core/IThrowable.cs"
tags: [clase, modulo/core]
---
# IThrowable

> A grabbable that can be launched on a ballistic arc while carried. The launch itself goes through `IGrabbable.Release` with `DropReason.Thrown`.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/IThrowable.cs`

## Hereda / implementa
[[IGrabbable]]

## Derivados / implementaciones
[[Animal]], [[Bomb]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IThrowable {
    <<interface>>
    +bool CanBeThrown
    +ThrowSettings ThrowSettings
    +float ThrowDistanceMultiplier
    +float CollisionRadius
    +float HalfHeight
  }
  class IGrabbable:::external
  <<interface>> IGrabbable
  class ThrowSettings:::external
  <<ScriptableObject>> ThrowSettings
  IThrowable --> ThrowSettings
  IThrowable ..|> IGrabbable
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IGrabbable]], [[ThrowSettings]]

## Usado por
[[Animal]], [[Bomb]], [[PlayerAim]], [[ThrowTrajectoryView]]
