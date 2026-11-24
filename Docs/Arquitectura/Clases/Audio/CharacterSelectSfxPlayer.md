---
modulo: Audio
namespace: AlienFarms.Audio
tipo: MonoBehaviour
archivo: "Assets/Scripts/Audio/CharacterSelectSfxPlayer.cs"
tags: [clase, modulo/audio]
---
# CharacterSelectSfxPlayer

**Módulo:** [[Modulo Audio|Audio]] · **Namespace:** `AlienFarms.Audio` · **Archivo:** `Assets/Scripts/Audio/CharacterSelectSfxPlayer.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterSelectSfxPlayer {
    <<MonoBehaviour>>
    -UiSfxLibrary _library
    -AudioSource[] _voices
  }
  class IRandomSource:::external
  <<interface>> IRandomSource
  class SfxVoicePool:::external
  class UiSfxLibrary:::external
  <<ScriptableObject>> UiSfxLibrary
  CharacterSelectSfxPlayer --> IRandomSource
  CharacterSelectSfxPlayer --> SfxVoicePool
  CharacterSelectSfxPlayer --> UiSfxLibrary
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[IRandomSource]], [[PlayerId]], [[SfxVoicePool]], [[SystemRandomSource]], [[UiSfxCue]], [[UiSfxLibrary]]

## Eventos que escucha
[[Eventos GameEvents#OnCharacterPickConfirmed|OnCharacterPickConfirmed]], [[Eventos GameEvents#OnCharacterPickReleased|OnCharacterPickReleased]]
