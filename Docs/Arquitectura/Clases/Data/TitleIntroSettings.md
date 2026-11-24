---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/TitleIntroSettings.cs"
tags: [clase, modulo/data]
---
# TitleIntroSettings

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/TitleIntroSettings.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class TitleIntroSettings {
    <<ScriptableObject>>
    -float _attractFadeOutDuration
    -float _menuFadeInDuration
    -float _menuFadeOutDuration
    -float _attractFadeInDuration
    -float _promptPulsePeriod
    -float _promptMinAlpha
    -float _promptMaxAlpha
    +float AttractFadeOutDuration
    +float MenuFadeInDuration
    +float MenuFadeOutDuration
    +float AttractFadeInDuration
    +CinemachineBlendDefinition CameraToMenuBlend
    +CinemachineBlendDefinition CameraToAttractBlend
    ...3 más
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[TitleIntroDirector]]
