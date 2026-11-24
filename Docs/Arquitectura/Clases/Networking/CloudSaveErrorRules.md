---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: static
archivo: "Assets/Scripts/Networking/Persistence/CloudSaveErrorRules.cs"
tags: [clase, modulo/networking]
---
# CloudSaveErrorRules

> Cloud Save failure reason → localization key (`PlayerProgressErrorKeys`) and whether to retry.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/CloudSaveErrorRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CloudSaveErrorRules {
    <<static>>
    +ToKey(CloudSaveExceptionReason reason)$ string
    +IsRetryable(CloudSaveExceptionReason reason)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerProgressErrorKeys]]

## Usado por
[[CloudSavePlayerStatsStore]]
