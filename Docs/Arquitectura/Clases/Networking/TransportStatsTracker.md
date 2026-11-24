---
modulo: Networking
namespace: AlienFarms.Networking.Diagnostics
tipo: class
archivo: "Assets/Scripts/Networking/Diagnostics/TransportStatsTracker.cs"
tags: [clase, modulo/networking]
---
# TransportStatsTracker

> Pure per-session state of the transport lines of `PoolStatsView`: detects a new session (the driver started), tracks the longest frame of the session and turns the driver's cumulative received-packet count into a rate. Ticked every frame (visible or not); the rate is re-based whenever the view asks (e.g. shown again), so a hidden period never shows up as one averaged sample. A pause (editor pause or step, application suspended) is not a frame: `NotifyPaused` drops the next frame from the longest-frame record and re-bases the rate.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Diagnostics` · **Archivo:** `Assets/Scripts/Networking/Diagnostics/TransportStatsTracker.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class TransportStatsTracker {
    +bool IsRunning
    +float LongestFrame
    +int RxPerSecond
    +Tick(bool running, float frameSeconds) bool
    +NotifyPaused() void
    +RequestRebase() void
    +SampleRx(ulong totalRxPackets, float now) int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PoolStatsView]]
