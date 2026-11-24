---
modulo: UI
namespace: AlienFarms.UI
tipo: enumeration
archivo: "Assets/Scripts/UI/ConnectionBanner.cs"
tags: [clase, modulo/ui]
---
# ConnectionBanner

> What the HUD connection banner shows for a seat (`ConnectionStatusRules.Resolve`).

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/ConnectionBanner.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ConnectionBanner {
    <<enumeration>>
    None
    Reconnecting
    Returned
    Left
    TimedOut
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`None`, `Reconnecting`, `Returned`, `Left`, `TimedOut`

## Usado por
[[ConnectionStatusRules]], [[ConnectionStatusView]]
