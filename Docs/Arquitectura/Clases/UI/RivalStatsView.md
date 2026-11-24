---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/RivalStatsView.cs"
tags: [clase, modulo/ui]
---
# RivalStatsView

> The rival's public stats in the online room ("Rival: {0} matches · {1} W · {2} D · {3} L"), read from the `PlayerProgressChannel`. Hidden while the channel has no rival stats. Redraws only when the channel changes (or the format resolves); TMP formats the numbers without allocating.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/RivalStatsView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class RivalStatsView {
    <<MonoBehaviour>>
    -PlayerProgressChannel _progress
    -LocalizedString _format
    -GameObject _root
    -TMP_Text _label
  }
  class LocalizedStringBinding:::external
  class PlayerProgressChannel:::external
  <<ScriptableObject>> PlayerProgressChannel
  RivalStatsView --> LocalizedStringBinding
  RivalStatsView --> PlayerProgressChannel
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LocalizationKeys]], [[LocalizedStringBinding]], [[PlayerProgressChannel]], [[PlayerStatsSummary]]
