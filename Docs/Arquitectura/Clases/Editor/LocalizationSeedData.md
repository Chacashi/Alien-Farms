---
modulo: Editor
namespace: AlienFarms.Editor
tipo: static
archivo: "Assets/Scripts/Editor/LocalizationSeedData.cs"
tags: [clase, modulo/editor]
---
# LocalizationSeedData

> Initial ES/EN values for every localization key. Spanish values match the texts authored in scenes/prefabs (so "Bind Selected" can match them). Only used to seed missing entries: edit translations in the tables.

**Módulo:** [[Modulo Editor|Editor]] · **Namespace:** `AlienFarms.Editor` · **Archivo:** `Assets/Scripts/Editor/LocalizationSeedData.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LocalizationSeedData {
    <<static>>
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LocalizationKeys]], [[LocalizationSeedEntry]]

## Usado por
[[LocalizationSetupMenu]]
