---
modulo: Input
namespace: AlienFarms.Input
tipo: class
archivo: "Assets/Scripts/Input/HoldToPauseTracker.cs"
tags: [clase, modulo/input]
---
# HoldToPauseTracker

> Plain C# hold-to-confirm timer: accumulates while a button is held and reports completion once. After completing, the button must be released before it can complete again; releasing early resets the progress.

**Módulo:** [[Modulo Input|Input]] · **Namespace:** `AlienFarms.Input` · **Archivo:** `Assets/Scripts/Input/HoldToPauseTracker.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class HoldToPauseTracker {
    +float Duration
    +float Progress
    +Tick(bool held, float unscaledDeltaTime) bool
    +Reset() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PlayerPauseInput]]
