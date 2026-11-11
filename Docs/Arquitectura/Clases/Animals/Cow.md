---
modulo: Animals
namespace: AlienFarms.Animals
tipo: class
archivo: "Assets/Scripts/Animals/Cow.cs"
tags: [clase, modulo/animals]
---
# Cow

> Heaviest animal: slow to react and to flee (straight line), worth the most points. Its carry and throw penalties live in its `AlienFarms.Data.AnimalData`.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/Cow.cs`

## Hereda / implementa
[[Animal]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class Cow {
    #ComputeFleeDirection(Vector3 awayFromThreat) Vector3
  }
  class Animal:::external
  <<abstract>> Animal
  Cow --|> Animal
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[Animal]]
