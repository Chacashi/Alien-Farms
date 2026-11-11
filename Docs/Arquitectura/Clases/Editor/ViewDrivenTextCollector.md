---
modulo: Editor
namespace: AlienFarms.Editor
tipo: static
archivo: "Assets/Scripts/Editor/ViewDrivenTextCollector.cs"
tags: [clase, modulo/editor]
---
# ViewDrivenTextCollector

> Finds TMP texts referenced by AlienFarms components (views write them from code, so they must never get a static `LocalizedText`). LocalizedText itself is ignored.

**Módulo:** [[Modulo Editor|Editor]] · **Namespace:** `AlienFarms.Editor` · **Archivo:** `Assets/Scripts/Editor/ViewDrivenTextCollector.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ViewDrivenTextCollector {
    <<static>>
    +Collect(GameObject root, HashSet~TMP_Text~ result)$ void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LocalizedText]]

## Usado por
[[LocalizationSetupMenu]]
