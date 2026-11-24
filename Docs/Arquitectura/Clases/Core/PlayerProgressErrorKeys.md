---
modulo: Core
namespace: AlienFarms.Core
tipo: static
archivo: "Assets/Scripts/Core/PlayerProgressErrorKeys.cs"
tags: [clase, modulo/core]
---
# PlayerProgressErrorKeys

> Localization keys (UI table) of the persistent progress (cloud stats) errors the cloud save service reports through the PlayerProgressChannel. Null / empty means "no error".

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/PlayerProgressErrorKeys.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerProgressErrorKeys {
    <<static>>
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[CloudSaveErrorRules]], [[CloudSavePlayerStatsStore]], [[LocalizationKeys]], [[PlayerStatsLedger]], [[PlayerStatsSync]], [[SimulatedFailureStatsStore]], [[StatsStoreResult]]
