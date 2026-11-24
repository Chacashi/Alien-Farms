---
modulo: Menu
namespace: AlienFarms.Menu
tipo: class
archivo: "Assets/Scripts/Menu/TitleIntroModel.cs"
tags: [clase, modulo/menu]
---
# TitleIntroModel

**Módulo:** [[Modulo Menu|Menu]] · **Namespace:** `AlienFarms.Menu` · **Archivo:** `Assets/Scripts/Menu/TitleIntroModel.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class TitleIntroModel {
    +TitleIntroState State
    +float AttractAlpha
    +float MenuAlpha
    +bool IsMenuInteractable
    +bool IsTransitioning
    +MenuCameraShot CameraTarget
    +SnapToAttract() void
    +SnapToMenu() void
    +RequestMenu() bool
    +RequestAttract() bool
    +Skip() bool
    +Tick(float dt, bool cameraSettledOnMenu, bool cameraSettledOnSky) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MenuCameraShot]], [[TitleIntroState]]

## Usado por
[[TitleIntroDirector]]
