---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/TitleStatsView.cs"
tags: [clase, modulo/ui]
---
# TitleStatsView

> Lifetime stats on the title screen, read from the `PlayerProgressChannel`. Shows a dash and a hint until stats are loaded (the cloud save service writes them from task 2.8). Redraws only when the channel changes.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/TitleStatsView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class TitleStatsView {
    <<MonoBehaviour>>
    -PlayerProgressChannel _progress
    -LocalizedString _emptyHint
    -TMP_Text[] _values
    -GameObject _placeholderRoot
    -TMP_Text _hint
    -GameObject _savedBadge
  }
  class LocalizedStringBinding:::external
  class PlayerProgressChannel:::external
  <<ScriptableObject>> PlayerProgressChannel
  TitleStatsView --> LocalizedStringBinding
  TitleStatsView --> PlayerProgressChannel
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LocalizationKeys]], [[LocalizedStringBinding]], [[PlayerProgressChannel]], [[PlayerProgressSaveStatus]], [[PlayerStatsSummary]]
