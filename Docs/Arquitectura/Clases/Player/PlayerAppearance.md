---
modulo: Player
namespace: AlienFarms.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Player/PlayerAppearance.cs"
tags: [clase, modulo/player]
---
# PlayerAppearance

> Applies the player's character visual by activating one of the pre-built variants under the Visual child (no instantiation) and deactivating the rest.

**Módulo:** [[Modulo Player|Player]] · **Namespace:** `AlienFarms.Player` · **Archivo:** `Assets/Scripts/Player/PlayerAppearance.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerAppearance {
    <<MonoBehaviour>>
    -CharacterVisual[] _variants
    +ApplyCharacter(CharacterDefinition character) void
  }
  class CharacterVisual:::external
  <<struct>> CharacterVisual
  PlayerAppearance --> CharacterVisual
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[CharacterVisual]]

## Usado por
[[PlayerRig]]
