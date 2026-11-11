---
modulo: Networking
namespace: AlienFarms.Networking.Diagnostics
tipo: enumeration
archivo: "Assets/Scripts/Networking/Diagnostics/DevConnectionPanel.cs"
tags: [clase, modulo/networking]
---
# Status

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Diagnostics` · **Archivo:** `Assets/Scripts/Networking/Diagnostics/DevConnectionPanel.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class Status {
    <<enumeration>>
    NoRoot
    Offline
    Starting
    Hosting
    Connecting
    Connected
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`NoRoot`, `Offline`, `Starting`, `Hosting`, `Connecting`, `Connected`

## Usa
[[CharacterRoster]], [[ConnectionPayload]], [[DirectConnection]], [[GameMode]], [[IConnectionStrategy]], [[MatchSession]], [[NetworkRootLifetime]], [[NetworkRuntimeAnchor]], [[NetworkSceneLoader]], [[NetworkSettings]], [[SceneFlow]], [[SceneNames]]

## Usado por
[[DevConnectionPanel]]
