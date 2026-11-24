---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/GameMode.cs"
tags: [clase, modulo/core]
---
# GameMode

> How the gameplay scene is driven: local split-screen or online.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/GameMode.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class GameMode {
    <<enumeration>>
    Local
    Online
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Local`, `Online`

## Usado por
[[DevConnectionPanel]], [[GameModeRouter]], [[JoinPromptView]], [[MatchSession]], [[OnlineModeRules]], [[OnlineSessionService]], [[SaveStatusBadgeView]], [[SessionExit]], [[Status]]
