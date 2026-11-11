---
modulo: Networking
namespace: AlienFarms.Networking.Prediction
tipo: struct
archivo: "Assets/Scripts/Networking/Prediction/InterpolatedPose.cs"
tags: [clase, modulo/networking]
---
# InterpolatedPose

> Pose sampled by `SnapshotInterpolator` at a render time.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Prediction` · **Archivo:** `Assets/Scripts/Networking/Prediction/InterpolatedPose.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class InterpolatedPose {
    <<struct>>
    +Vector3 Position
    +Vector3 Velocity
    +float FacingYaw
    +float StunRemaining
    +PlayerSnapshotFlags Flags
    +bool IsClamped
    +Vector3 FacingDirection
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerSnapshotFlags]]

## Usado por
[[NetworkPlayerMotion]], [[SnapshotInterpolator]]
