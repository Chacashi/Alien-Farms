---
modulo: Animals
namespace: AlienFarms.Animals
tipo: class
archivo: "Assets/Scripts/Animals/Chicken.cs"
tags: [clase, modulo/animals]
---
# Chicken

> Fast, light animal that zigzags while fleeing.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/Chicken.cs`

## Hereda / implementa
[[Animal]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class Chicken {
    #OnInitialized() void
    #ComputeFleeDirection(Vector3 awayFromThreat) Vector3
  }
  class Animal:::external
  <<abstract>> Animal
  class ChickenData:::external
  Chicken --> ChickenData
  Chicken --|> Animal
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]], [[ChickenData]]
