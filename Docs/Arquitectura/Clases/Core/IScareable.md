---
modulo: Core
namespace: AlienFarms.Core
tipo: interface
archivo: "Assets/Scripts/Core/IScareable.cs"
tags: [clase, modulo/core]
---
# IScareable

> Something that runs away from a sudden threat (e.g. a blast). Only free, grounded animals react.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/IScareable.cs`

## Derivados / implementaciones
[[Animal]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IScareable {
    <<interface>>
    +Scare(Vector3 threatPosition, float minDuration) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[Animal]], [[Bomb]]
