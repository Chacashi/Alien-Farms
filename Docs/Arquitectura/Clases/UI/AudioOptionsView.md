---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/AudioOptionsView.cs"
tags: [clase, modulo/ui]
---
# AudioOptionsView

> Audio options panel: Master/Music/SFX sliders (linear 0..1), optional mute toggle, percentage labels and SFX preview. Reads the service when shown, writes on every change and saves when hidden. Lives on the panel root.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/AudioOptionsView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AudioOptionsView {
    <<MonoBehaviour>>
    -AudioVolumeService _service
    -Slider _master
    -Slider _music
    -Slider _sfx
    -Toggle _mute
    -TMP_Text _masterValue
    -TMP_Text _musicValue
    -TMP_Text _sfxValue
    -AudioSource _sfxPreview
    -AudioClip _sfxPreviewClip
    -UnityAction~float~ _masterChanged
  }
  class AudioVolumeService:::external
  <<MonoBehaviour>> AudioVolumeService
  AudioOptionsView --> AudioVolumeService
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AudioVolumeService]], [[VolumeChannel]]
