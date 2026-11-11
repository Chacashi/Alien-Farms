---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/MatchTimerView.cs"
tags: [clase, modulo/ui]
---
# MatchTimerView

> Shows the remaining round time as "m:ss". Updated only when the whole second changes (no per-frame work).

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/MatchTimerView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchTimerView {
    <<MonoBehaviour>>
    -TMP_Text _text
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[TimeFormat]]

## Eventos que escucha
[[Eventos GameEvents#OnMatchTimeChanged|OnMatchTimeChanged]]
