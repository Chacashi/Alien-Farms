---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/CharacterRoster.cs"
tags: [clase, modulo/data]
---
# CharacterRoster

> Ordered list of selectable characters (character select order).

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/CharacterRoster.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterRoster {
    <<ScriptableObject>>
    -CharacterDefinition[] _characters
    +int Count
    +Get(int index) CharacterDefinition
    +IndexOf(CharacterDefinition character) int
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  CharacterRoster --> CharacterDefinition
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]]

## Usado por
[[MenuFlowController]], [[NetworkLobbyState]], [[NetworkPlayer]], [[NetworkPlayerRoster]], [[OnlineMenuController]]
