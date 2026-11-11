---
modulo: Animals
namespace: AlienFarms.Animals
tipo: static
archivo: "Assets/Scripts/Animals/AnimalSteering.cs"
tags: [clase, modulo/animals]
---
# AnimalSteering

> Pure planar (X/Z) steering helpers that keep animals inside a `Bounds` shrunk by a margin (the "inner rectangle"). Stateless, allocation-free and testable in EditMode.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/AnimalSteering.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AnimalSteering {
    <<static>>
    +ConfineDirection(Vector3 position, Vector3 direction, Bounds bounds, float margin, float lookahead)$ Vector3
    +EscapeAlongEdge(Vector3 position, Vector3 awayFromThreat, Bounds bounds, float margin, float lookahead)$ Vector3
    +SteerFlee()$ Vector3
    +RandomPointInside(Vector3 around, float radius, Bounds bounds, float margin)$ Vector3
    +ClampInside(Vector3 point, Bounds bounds, float margin)$ Vector3
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CornerEscape]]

## Usado por
[[Animal]], [[AnimalSpawner]]
