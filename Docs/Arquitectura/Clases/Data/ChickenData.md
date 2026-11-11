---
modulo: Data
namespace: AlienFarms.Data
tipo: class
archivo: "Assets/Scripts/Data/ChickenData.cs"
tags: [clase, modulo/data]
---
# ChickenData

> Animal data for chickens: adds a zigzag while fleeing.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/ChickenData.cs`

## Hereda / implementa
[[AnimalData]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class ChickenData {
    -float _zigzagAngle
    -float _zigzagFrequency
    +float ZigzagAngle
    +float ZigzagFrequency
  }
  class AnimalData:::external
  <<ScriptableObject>> AnimalData
  ChickenData --|> AnimalData
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AnimalData]]

## Usado por
[[Chicken]]
