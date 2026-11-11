---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/ScoreView.cs"
tags: [clase, modulo/ui]
---
# ScoreView

> Shows one player's score. Updated only on score changes.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/ScoreView.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ScoreView {
    <<MonoBehaviour>>
    -PlayerId _player
    -string _format
    -TMP_Text _text
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameEvents]], [[PlayerId]]

## Eventos que escucha
[[Eventos GameEvents#OnScoreChanged|OnScoreChanged]]
