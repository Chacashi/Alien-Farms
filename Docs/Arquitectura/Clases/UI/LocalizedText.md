---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/LocalizedText.cs"
tags: [clase, modulo/ui]
---
# LocalizedText

> Writes a localized string into the TMP text on this object and refreshes it when the locale changes. Only for static texts: never put it on a text that a view writes from code.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/LocalizedText.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LocalizedText {
    <<MonoBehaviour>>
    -LocalizedString _string
    +LocalizedString String
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[LocalizationSetupMenu]], [[ViewDrivenTextCollector]]
