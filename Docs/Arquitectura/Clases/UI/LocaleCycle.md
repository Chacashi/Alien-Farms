---
modulo: UI
namespace: AlienFarms.UI
tipo: static
archivo: "Assets/Scripts/UI/LocaleCycle.cs"
tags: [clase, modulo/ui]
---
# LocaleCycle

> Index math for cycling through a list of locales with wrap-around.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/LocaleCycle.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LocaleCycle {
    <<static>>
    +Next(int current, int count, int step)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[LanguageSelector]]
