---
modulo: Menu
namespace: AlienFarms.Menu
tipo: MonoBehaviour
archivo: "Assets/Scripts/Menu/TitleIntroDirector.cs"
tags: [clase, modulo/menu]
---
# TitleIntroDirector

**Módulo:** [[Modulo Menu|Menu]] · **Namespace:** `AlienFarms.Menu` · **Archivo:** `Assets/Scripts/Menu/TitleIntroDirector.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class TitleIntroDirector {
    <<MonoBehaviour>>
    -TitleIntroSettings _settings
    -TitleScreenView _view
    -MenuCameraRig _cameraRig
    +event Action MenuReady
    +TitleIntroState State
    +bool IsMenuInteractable
    +bool IsTransitioning
    +SnapToAttract() void
    +SnapToMenu() void
    +PlayToMenu() void
    +PlayToAttract() void
    +Skip() void
  }
  class MenuCameraRig:::external
  <<MonoBehaviour>> MenuCameraRig
  class TitleIntroModel:::external
  class TitleIntroSettings:::external
  <<ScriptableObject>> TitleIntroSettings
  class TitleScreenView:::external
  <<MonoBehaviour>> TitleScreenView
  TitleIntroDirector --> MenuCameraRig
  TitleIntroDirector --> TitleIntroModel
  TitleIntroDirector --> TitleIntroSettings
  TitleIntroDirector --> TitleScreenView
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[MenuCameraRig]], [[MenuCameraShot]], [[TitleIntroModel]], [[TitleIntroSettings]], [[TitleIntroState]], [[TitlePromptPulse]], [[TitleScreenView]]

## Usado por
[[MenuFlowController]]
