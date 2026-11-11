---
modulo: Player
namespace: AlienFarms.Player
tipo: struct
archivo: "Assets/Scripts/Player/CharacterVisual.cs"
tags: [clase, modulo/player]
---
# CharacterVisual

> Pairs a character with its pre-built visual root inside the Player prefab.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/CharacterVisual.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterVisual {
    <<struct>>
    -CharacterDefinition _character
    -GameObject _root
    +CharacterDefinition Character
    +GameObject Root
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  CharacterVisual --> CharacterDefinition
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]]

## Usado por
[[PlayerAppearance]]
