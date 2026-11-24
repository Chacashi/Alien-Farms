---
modulo: Animals
namespace: AlienFarms.Animals
tipo: enumeration
archivo: "Assets/Scripts/Animals/AnimalState.cs"
tags: [clase, modulo/animals]
---
# AnimalState

> High-level behaviour state of an `Animal`.

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/AnimalState.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AnimalState {
    <<enumeration>>
    Inactive
    Wandering
    Fleeing
    Carried
    Captured
    Airborne
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Inactive`, `Wandering`, `Fleeing`, `Carried`, `Captured`, `Airborne`

## Usado por
[[Animal]], [[AnimalNetState]], [[AnimalSpawner]], [[CaptureRules]], [[MatchScore]], [[NetworkAnimal]]
