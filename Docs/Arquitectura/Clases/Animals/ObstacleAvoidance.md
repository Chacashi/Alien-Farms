---
modulo: Animals
namespace: AlienFarms.Animals
tipo: static
archivo: "Assets/Scripts/Animals/ObstacleAvoidance.cs"
tags: [clase, modulo/animals]
---
# ObstacleAvoidance

> Pure planar obstacle avoidance: given the desired direction and a probe hit in front of the animal, slides along the obstacle tangent and adds a small repulsion that grows as the hit gets closer. Stateless apart from the `AvoidanceSide` passed by ref; allocation-free and EditMode-testable.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/ObstacleAvoidance.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleAvoidance {
    <<static>>
    +Avoid()$ Vector3
    +Detour(Vector3 desired, Vector3 hitNormal, float hitDistance, float probeDistance, int side)$ Vector3
    +SideOf(Vector3 direction, Vector3 hitNormal)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AvoidanceSide]]

## Usado por
[[Animal]]
