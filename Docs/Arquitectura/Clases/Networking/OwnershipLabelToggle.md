---
modulo: Networking
namespace: AlienFarms.Networking.Diagnostics
tipo: static
archivo: "Assets/Scripts/Networking/Diagnostics/OwnershipLabelToggle.cs"
tags: [clase, modulo/networking]
---
# OwnershipLabelToggle

> Machine-local switch of the ownership debug labels (not replicated, no PlayerId: it is a debug view of this screen). Without an override the labels follow NetworkSettings.ShowOwnershipLabels; the debug overlay (F1–F4, task 3.3) calls `Toggle` / `SetVisible` and every label refreshes through `Changed`. Reset on play-mode entry (no domain reload).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Diagnostics` · **Archivo:** `Assets/Scripts/Networking/Diagnostics/OwnershipLabelToggle.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OwnershipLabelToggle {
    <<static>>
    +bool HasOverride$
    +IsVisible(bool defaultVisible)$ bool
    +SetVisible(bool visible)$ void
    +Toggle(bool defaultVisible)$ void
    +ClearOverride()$ void
  }
  class DebugViewSwitch:::external
  OwnershipLabelToggle --> DebugViewSwitch
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[DebugViewSwitch]]

## Usado por
[[CarriableOwnershipLabel]]
