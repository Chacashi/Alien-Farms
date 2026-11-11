---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: class
archivo: "Assets/Scripts/Networking/Player/NetworkCommandSource.cs"
tags: [clase, modulo/networking]
---
# NetworkCommandSource

> `IPlayerCommandSource` fed with network commands (server side of a remote player). Holds the last applied command and raises `GrabStarted` / `GrabCanceled` on the Grab bit's edges. Look is always zero: the view arrives as an absolute yaw/pitch and is applied to the camera with SetDrivenView.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/NetworkCommandSource.cs`

## Hereda / implementa
[[IPlayerCommandSource]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkCommandSource {
    +event Action GrabStarted
    +event Action GrabCanceled
    +Vector2 Move
    +Vector2 Look
    +bool IsLookPointerDelta
    +bool IsSprintHeld
    +bool IsGrabHeld
    +bool IsAimHeld
    +Apply(PlayerInputCommand command) void
    +Reset() void
  }
  class IPlayerCommandSource:::external
  <<interface>> IPlayerCommandSource
  NetworkCommandSource ..|> IPlayerCommandSource
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IPlayerCommandSource]], [[PlayerInputCommand]]

## Usado por
[[Mode]], [[NetworkPlayerInput]]
