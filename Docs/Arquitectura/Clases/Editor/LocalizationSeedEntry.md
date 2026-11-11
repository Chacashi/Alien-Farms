---
modulo: Editor
namespace: AlienFarms.Editor
tipo: struct
archivo: "Assets/Scripts/Editor/LocalizationSeedEntry.cs"
tags: [clase, modulo/editor]
---
# LocalizationSeedEntry

> One string table entry seeded by `LocalizationSetupMenu`.

**Módulo:** [[Modulo Editor|Editor]] · **Namespace:** `AlienFarms.Editor` · **Archivo:** `Assets/Scripts/Editor/LocalizationSeedEntry.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LocalizationSeedEntry {
    <<struct>>
    +string Table
    +string Key
    +string Spanish
    +string English
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[LocalizationSeedData]], [[LocalizationSetupMenu]]
