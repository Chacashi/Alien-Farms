---
modulo: Menu
namespace: AlienFarms.Menu
tipo: MonoBehaviour
archivo: "Assets/Scripts/Menu/MenuCameraRig.cs"
tags: [clase, modulo/menu]
---
# MenuCameraRig

**Módulo:** [[Modulo Menu|Menu]] · **Namespace:** `AlienFarms.Menu` · **Archivo:** `Assets/Scripts/Menu/MenuCameraRig.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MenuCameraRig {
    <<MonoBehaviour>>
    -CinemachineBrain _brain
    -CinemachineCamera _skyCamera
    -CinemachineCamera _menuCamera
    +Show(MenuCameraShot shot, CinemachineBlendDefinition blend) void
    +Cut(MenuCameraShot shot) void
    +FinishBlend() void
    +IsSettledOn(MenuCameraShot shot) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MenuCameraShot]]

## Usado por
[[TitleIntroDirector]]
