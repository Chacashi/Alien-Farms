---
modulo: Input
namespace: AlienFarms.Input
tipo: class
archivo: "Assets/Scripts/Input/AxisStepDetector.cs"
tags: [clase, modulo/input]
---
# AxisStepDetector

> Plain C# edge detector that turns a continuous axis (stick, dpad) into discrete steps: -1 or +1 once when the axis crosses the threshold, 0 otherwise. Holding does not repeat; returning inside the threshold re-arms it.

**Módulo:** [[Modulo Input|Input]] · **Namespace:** `AlienFarms.Input` · **Archivo:** `Assets/Scripts/Input/AxisStepDetector.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AxisStepDetector {
    +Update(float axis, float threshold) int
    +Reset() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PlayerMenuInput]]
