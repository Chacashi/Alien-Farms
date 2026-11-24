---
modulo: Networking
namespace: AlienFarms.Networking.Diagnostics
tipo: static
archivo: "Assets/Scripts/Networking/Diagnostics/PoolStatsToggle.cs"
tags: [clase, modulo/networking]
---
# PoolStatsToggle

> Machine-local switch of the pool / transport stats view (`PoolStatsView`). Without an override it follows NetworkSettings.ShowPoolStats; the debug overlay (F1–F4, task 3.3) calls `Toggle` / `SetVisible`. Same pattern as `OwnershipLabelToggle`. Reset on play-mode entry.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Diagnostics` · **Archivo:** `Assets/Scripts/Networking/Diagnostics/PoolStatsToggle.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PoolStatsToggle {
    <<static>>
    +bool HasOverride$
    +IsVisible(bool defaultVisible)$ bool
    +SetVisible(bool visible)$ void
    +Toggle(bool defaultVisible)$ void
    +ClearOverride()$ void
  }
  class DebugViewSwitch:::external
  PoolStatsToggle --> DebugViewSwitch
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[DebugViewSwitch]]

## Usado por
[[PoolStatsView]]
