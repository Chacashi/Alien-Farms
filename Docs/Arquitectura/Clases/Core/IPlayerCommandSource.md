---
modulo: Core
namespace: AlienFarms.Core
tipo: interface
archivo: "Assets/Scripts/Core/IPlayerCommandSource.cs"
tags: [clase, modulo/core]
---
# IPlayerCommandSource

> Everything the player's gameplay components read from input. Implemented locally by the PlayerInputHandler and, online, by a network-fed source. Values are sampled every frame; events fire on button edges.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/IPlayerCommandSource.cs`

## Derivados / implementaciones
[[NetworkCommandSource]], [[PlayerInputHandler]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IPlayerCommandSource {
    <<interface>>
    +Vector2 Move
    +Vector2 Look
    +bool IsLookPointerDelta
    +bool IsSprintHeld
    +bool IsGrabHeld
    +bool IsAimHeld
    -event Action GrabStarted
    -event Action GrabCanceled
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[NetworkCommandSource]], [[PlayerAim]], [[PlayerGrabber]], [[PlayerInputHandler]], [[PlayerMovement]], [[ThirdPersonCamera]]
