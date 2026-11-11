---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/MenuSettings.cs"
tags: [clase, modulo/data]
---
# MenuSettings

> Menu navigation tuning shared by per-player menu input.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/MenuSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MenuSettings {
    <<ScriptableObject>>
    -float _navigateStepThreshold
    -float _loadDelayAfterConfirm
    +float NavigateStepThreshold
    +float LoadDelayAfterConfirm
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[MenuFlowController]], [[PlayerMenuInput]]
