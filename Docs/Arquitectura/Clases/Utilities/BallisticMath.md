---
modulo: Utilities
namespace: AlienFarms.Utilities
tipo: static
archivo: "Assets/Scripts/Utilities/BallisticMath.cs"
tags: [clase, modulo/utilities]
---
# BallisticMath

> Pure ballistic helpers shared by the throw and its preview. `Step` integrates exactly like PhysX (semi-implicit Euler), so a preview stepped with Time.fixedDeltaTime matches an undamped Rigidbody.

**Módulo:** [[Modulo Utilities|Utilities]] · **Namespace:** `AlienFarms.Utilities` · **Archivo:** `Assets/Scripts/Utilities/BallisticMath.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BallisticMath {
    <<static>>
    +ElevationFromPitch()$ float
    +LaunchVelocity(Vector3 planarForward, float elevationDeg, float speed)$ Vector3
    +SpeedScaleForDistance(float distanceMultiplier)$ float
    +Step(Vector3 position, Vector3 velocity, Vector3 gravity, float dt)$ void
    +PreviewPointCount(float maxTime, float dt, int stepsPerPoint)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PlayerAim]], [[ThrowTrajectoryView]]
