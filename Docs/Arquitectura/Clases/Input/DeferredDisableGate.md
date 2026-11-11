---
modulo: Input
namespace: AlienFarms.Input
tipo: class
archivo: "Assets/Scripts/Input/DeferredDisableGate.cs"
tags: [clase, modulo/input]
---
# DeferredDisableGate

> Plain C# helper for a component that may be enabled at once but must only be disabled later (e.g. an EventSystem that cannot be disabled from inside its own Update). A later enable request cancels a pending disable.

**Módulo:** [[Modulo Input|Input]] · **Namespace:** `AlienFarms.Input` · **Archivo:** `Assets/Scripts/Input/DeferredDisableGate.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class DeferredDisableGate {
    +bool IsDisablePending
    +Request(bool active, bool isCurrentlyEnabled) bool
    +ConsumePendingDisable() bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PlayerMenuInput]]
