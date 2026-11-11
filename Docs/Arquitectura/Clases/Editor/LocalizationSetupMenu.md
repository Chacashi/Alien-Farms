---
modulo: Editor
namespace: AlienFarms.Editor
tipo: static
archivo: "Assets/Scripts/Editor/LocalizationSetupMenu.cs"
tags: [clase, modulo/editor]
---
# LocalizationSetupMenu

> Idempotent localization setup: creates whatever is missing (settings, es/en locales, UI/HowToPlay tables and their entries) and enforces the startup configuration. Never overwrites existing translations. Also offers a helper that adds `LocalizedText` to selected TMP texts by matching their Spanish text.

**Módulo:** [[Modulo Editor|Editor]] · **Namespace:** `AlienFarms.Editor` · **Archivo:** `Assets/Scripts/Editor/LocalizationSetupMenu.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LocalizationSetupMenu {
    <<static>>
    +Setup()$ void
    +BindSelected()$ void
    +SetLocalizedString(LocalizedText target, string table, string key)$ void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LocalizationKeys]], [[LocalizationSeedData]], [[LocalizationSeedEntry]], [[LocalizedText]], [[ViewDrivenTextCollector]]
