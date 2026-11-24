---
modulo: UI
namespace: AlienFarms.UI
tipo: MonoBehaviour
archivo: "Assets/Scripts/UI/CharacterCardButton.cs"
tags: [clase, modulo/ui]
---
# CharacterCardButton

> Makes a `CharacterCardView` a navigable button (online room): selecting it reports the hovered character and clicking (Submit) reports the pick. The card itself still renders cursors and locks.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/CharacterCardButton.cs`

## Hereda / implementa
`MonoBehaviour`, `ISelectHandler`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterCardButton {
    <<MonoBehaviour>>
    -CharacterCardView _card
    +event Action~CharacterDefinition~ Clicked
    +event Action~CharacterDefinition~ Selected
    +CharacterCardView Card
    +CharacterDefinition Character
    +bool Interactable
    +OnSelect(BaseEventData eventData) void
  }
  class CharacterCardView:::external
  <<MonoBehaviour>> CharacterCardView
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  CharacterCardButton --> CharacterCardView
  CharacterCardButton --> CharacterDefinition
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterCardView]], [[CharacterDefinition]]

## Usado por
[[OnlinePanelView]]
