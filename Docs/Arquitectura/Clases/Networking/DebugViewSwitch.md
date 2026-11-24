---
modulo: Networking
namespace: AlienFarms.Networking.Diagnostics
tipo: class
archivo: "Assets/Scripts/Networking/Diagnostics/DebugViewSwitch.cs"
tags: [clase, modulo/networking]
---
# DebugViewSwitch

> Machine-local visibility switch of one debug view: without an override the view follows its default (a NetworkSettings flag); the debug overlay (F1–F4, task 3.3) sets or flips the override and listeners re-read `IsVisible` on `Changed`. Pure C#; the static facades (`OwnershipLabelToggle`, `PoolStatsToggle`) own one each and reset it on play-mode entry.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Diagnostics` · **Archivo:** `Assets/Scripts/Networking/Diagnostics/DebugViewSwitch.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class DebugViewSwitch {
    +event Action Changed
    +bool HasOverride
    +IsVisible(bool defaultVisible) bool
    +SetVisible(bool visible) void
    +Toggle(bool defaultVisible) void
    +ClearOverride() void
    +Reset() void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[OwnershipLabelToggle]], [[PoolStatsToggle]]
