---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: static
archivo: "Assets/Scripts/Networking/Player/ThrowViewRules.cs"
tags: [clase, modulo/networking]
---
# ThrowViewRules

> Server check of a remote throw: the view the owner reports must be close to the last view the server knows for that player (from its input commands), within a tolerance on yaw (wrapped) and pitch.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/ThrowViewRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ThrowViewRules {
    <<static>>
    +IsWithinTolerance(float yaw, float pitch, float knownYaw, float knownPitch, float toleranceDegrees)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[Mode]], [[NetworkCarry]]
