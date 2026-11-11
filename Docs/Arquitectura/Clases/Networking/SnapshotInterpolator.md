---
modulo: Networking
namespace: AlienFarms.Networking.Prediction
tipo: static
archivo: "Assets/Scripts/Networking/Prediction/SnapshotInterpolator.cs"
tags: [clase, modulo/networking]
---
# SnapshotInterpolator

> Samples a `SnapshotBuffer` at a render time (server clock): linear between the two snapshots around it, clamped to the oldest / newest outside the range (no extrapolation). Teleports never blend: the buffer only holds snapshots of one teleport epoch.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Prediction` · **Archivo:** `Assets/Scripts/Networking/Prediction/SnapshotInterpolator.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SnapshotInterpolator {
    <<static>>
    +TrySample(SnapshotBuffer buffer, double renderTime, InterpolatedPose pose)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[InterpolatedPose]], [[PlayerSnapshot]], [[SnapshotBuffer]]

## Usado por
[[NetworkPlayerMotion]]
