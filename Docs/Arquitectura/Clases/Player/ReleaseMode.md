---
modulo: Player
namespace: AlienFarms.Player
tipo: enumeration
archivo: "Assets/Scripts/Player/ReleaseMode.cs"
tags: [clase, modulo/player]
---
# ReleaseMode

> Who decides when a `PlayerGrabber` lets go of what it carries.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/ReleaseMode.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ReleaseMode {
    <<enumeration>>
    Auto
    External
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Auto`, `External`

## Usado por
[[Mode]], [[NetworkCarry]], [[PlayerGrabber]]
