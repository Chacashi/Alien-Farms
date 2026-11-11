---
modulo: Core
namespace: AlienFarms.Core
tipo: interface
archivo: "Assets/Scripts/Core/IZoneResolver.cs"
tags: [clase, modulo/core]
---
# IZoneResolver

> Answers which player zone (if any) contains a world position.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/IZoneResolver.cs`

## Derivados / implementaciones
[[PlayerZoneRegistry]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IZoneResolver {
    <<interface>>
    +TryGetZoneAt(Vector3 position, PlayerId owner, Bounds area) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]]

## Usado por
[[Animal]], [[AnimalSpawner]], [[AnimalTypePool]], [[PlayerZoneRegistry]]
