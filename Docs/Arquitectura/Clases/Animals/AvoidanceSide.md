---
modulo: Animals
namespace: AlienFarms.Animals
tipo: struct
archivo: "Assets/Scripts/Animals/AvoidanceSide.cs"
tags: [clase, modulo/animals]
---
# AvoidanceSide

> Hysteresis state for `ObstacleAvoidance.Avoid`: once an animal picks a side to go around an obstacle, it keeps that side for that same obstacle until `Until`. The side survives expiry (it is reused when no hint is available) until `Clear`. Plain value type, passed by ref; the default value is "inactive, no side".

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/AvoidanceSide.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AvoidanceSide {
    <<struct>>
    +int Side
    +float Until
    +int ObstacleId
    +IsActive(float now) bool
    +Holds(float now, int obstacleId) bool
    +Begin(int side, float until, int obstacleId) void
    +Clear() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[Animal]], [[ObstacleAvoidance]]
