---
modulo: Core
namespace: AlienFarms.Core
tipo: static
archivo: "Assets/Scripts/Core/SceneFlow.cs"
tags: [clase, modulo/core]
---
# SceneFlow

> Stateless scene transitions. Every load restores time and audio first, so leaving a paused scene never carries `Time.timeScale` = 0 or a paused `AudioListener` into the next one.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/SceneFlow.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SceneFlow {
    <<static>>
    +LoadMainMenu()$ bool
    +LoadGameplay()$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[SceneNames]]

## Usado por
[[DevConnectionPanel]], [[LocalMatchExitHandler]], [[MenuFlowController]], [[OnlineMatchBootstrap]], [[OnlineMatchExitHandler]], [[Status]]
