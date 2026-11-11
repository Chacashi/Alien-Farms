---
modulo: Menu
namespace: AlienFarms.Menu
tipo: class
archivo: "Assets/Scripts/Menu/CharacterSelection.cs"
tags: [clase, modulo/menu]
---
# CharacterSelection

> Plain C# model of the character select screen: which players joined, which character each one hovers and which characters are locked (confirmed). A character can be locked by a single player; locked characters are skipped by the other players' cursors.

**Módulo:** [[Modulo Menu|Menu]] · **Namespace:** `AlienFarms.Menu` · **Archivo:** `Assets/Scripts/Menu/CharacterSelection.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterSelection {
    +int CharacterCount
    +int MaxPlayers
    +int JoinedCount
    +bool AllConfirmed
    +IsJoined(PlayerId id) bool
    +IsConfirmed(PlayerId id) bool
    +GetHover(PlayerId id) int
    +GetConfirmedIndex(PlayerId id) int
    +LockedBy(int characterIndex) PlayerId
    +Join(PlayerId id) bool
    +Leave(PlayerId id) bool
    +Move(PlayerId id, int direction) bool
    +Confirm(PlayerId id) bool
    +Cancel(PlayerId id) bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]]

## Usado por
[[MenuFlowController]]
