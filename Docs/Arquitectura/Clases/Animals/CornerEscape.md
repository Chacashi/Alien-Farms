---
modulo: Animals
namespace: AlienFarms.Animals
tipo: struct
archivo: "Assets/Scripts/Animals/CornerEscape.cs"
tags: [clase, modulo/animals]
---
# CornerEscape

> Hysteresis state for `AnimalSteering.SteerFlee`: once a cornered animal picks an edge to run along, it keeps that axis direction at least until `Until` and, while still cornered, until the axis gets blocked. Plain value type, passed by ref; the default value is "inactive".

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/CornerEscape.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CornerEscape {
    <<struct>>
    +Vector3 Direction
    +float Until
    +IsActive(float now) bool
    +Begin(Vector3 direction, float until) void
    +Clear() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[Animal]], [[AnimalSteering]]
