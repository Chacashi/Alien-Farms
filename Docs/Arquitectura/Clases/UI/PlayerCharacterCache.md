---
modulo: UI
namespace: AlienFarms.UI
tipo: class
archivo: "Assets/Scripts/UI/PlayerCharacterCache.cs"
tags: [clase, modulo/ui]
---
# PlayerCharacterCache

> Plain C# map from `PlayerId` to the character assigned to that player (views keep one each).

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/PlayerCharacterCache.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerCharacterCache {
    +Set(PlayerId id, CharacterDefinition character) void
    +Get(PlayerId id) CharacterDefinition
    +Clear() void
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  PlayerCharacterCache --> CharacterDefinition
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[PlayerId]]

## Usado por
[[ConnectionStatusView]], [[MatchResultsView]], [[PauseMenuView]]
