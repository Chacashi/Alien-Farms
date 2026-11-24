---
modulo: Utilities
namespace: AlienFarms.Utilities
tipo: MonoBehaviour
archivo: "Assets/Scripts/Utilities/ConstantRotation.cs"
tags: [clase, modulo/utilities]
---
# ConstantRotation

> Spins its transform around a local axis at a constant rate (windmill blades, props). Purely visual: not networked, every machine rotates it locally.

**Módulo:** [[Modulo Utilities|Utilities]] · **Namespace:** `AlienFarms.Utilities` · **Archivo:** `Assets/Scripts/Utilities/ConstantRotation.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ConstantRotation {
    <<MonoBehaviour>>
    -Vector3 _localAxis
    -float _degreesPerSecond
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```
