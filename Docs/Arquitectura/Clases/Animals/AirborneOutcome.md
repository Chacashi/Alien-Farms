---
modulo: Animals
namespace: AlienFarms.Animals
tipo: enumeration
archivo: "Assets/Scripts/Animals/AirborneOutcome.cs"
tags: [clase, modulo/animals]
---
# AirborneOutcome

> What an airborne animal does this physics step (see `AnimalLanding.Evaluate`).

**Módulo:** [[Modulo Animals|Animals]] · **Namespace:** `AlienFarms.Animals` · **Archivo:** `Assets/Scripts/Animals/AirborneOutcome.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AirborneOutcome {
    <<enumeration>>
    Keep
    Land
    Despawn
    SlideOff
    Relocate
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Keep`, `Land`, `Despawn`, `SlideOff`, `Relocate`

## Usado por
[[Animal]], [[AnimalLanding]]
