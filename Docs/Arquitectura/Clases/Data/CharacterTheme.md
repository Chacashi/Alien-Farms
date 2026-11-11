---
modulo: Data
namespace: AlienFarms.Data
tipo: struct
archivo: "Assets/Scripts/Data/CharacterTheme.cs"
tags: [clase, modulo/data]
---
# CharacterTheme

> Colors that identify a playable character in the HUD and menus.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/CharacterTheme.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterTheme {
    <<struct>>
    -Color _primary
    -Color _secondary
    -Color _text
    -Color _accent
    +Color Primary
    +Color Secondary
    +Color Text
    +Color Accent
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[CharacterDefinition]], [[PlayerHudThemeView]]
