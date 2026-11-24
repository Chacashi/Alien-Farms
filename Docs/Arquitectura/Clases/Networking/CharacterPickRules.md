---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: static
archivo: "Assets/Scripts/Networking/Player/CharacterPickRules.cs"
tags: [clase, modulo/networking]
---
# CharacterPickRules

> Pure rule of the default character of a seat that reached the gameplay scene without a pick (e.g. a new client taking a seat freed mid-match): the seat's own index, or the next free one when the other seat already uses it.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/CharacterPickRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CharacterPickRules {
    <<static>>
    +Pick(int seatIndex, int otherCharacter, int rosterCount)$ int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[NetworkPlayerRoster]]
