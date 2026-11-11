---
modulo: Animals
namespace: AlienFarms.Animals
tipo: class
archivo: "Assets/Scripts/Animals/Sheep.cs"
tags: [clase, modulo/animals]
---
# Sheep

> Heavy, slow animal that flees in a straight line.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/Sheep.cs`

## Hereda / implementa
[[Animal]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class Sheep {
    #ComputeFleeDirection(Vector3 awayFromThreat) Vector3
  }
  class Animal:::external
  <<abstract>> Animal
  Sheep --|> Animal
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]]
