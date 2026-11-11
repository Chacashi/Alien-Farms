---
modulo: Utilities
namespace: AlienFarms.Utilities
tipo: static
archivo: "Assets/Scripts/Utilities/PlanarMath.cs"
tags: [clase, modulo/utilities]
---
# PlanarMath

> Pure geometric helpers on the horizontal (XZ) plane.

**Módulo:** [[Modulo Utilities|Utilities]] · **Namespace:** `AlienFarms.Utilities` · **Archivo:** `Assets/Scripts/Utilities/PlanarMath.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlanarMath {
    <<static>>
    +KnockbackDirection(Vector3 center, Vector3 target, Vector3 fallback)$ Vector3
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PlayerStunReceiver]]
