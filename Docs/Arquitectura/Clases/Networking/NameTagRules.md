---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: static
archivo: "Assets/Scripts/Networking/Player/NameTagRules.cs"
tags: [clase, modulo/networking]
---
# NameTagRules

> Pure rules of `NetworkNameTag`: when an avatar's tag shows, which name and color it uses and the debug "client N" line (course topic 9: the reconnected player gets a new client id).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/NameTagRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NameTagRules {
    <<static>>
    +IsVisible(bool isSpawned, bool showNameTags, bool isLocalOwner, PlayerId seat)$ bool
    +ResolveName(string displayName, PlayerId seat)$ string
    +ResolveColor(CharacterDefinition character, Color fallback)$ Color
    +ClientDetail(bool showClientId, ulong clientId)$ string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CarriableOwnerRules]], [[CharacterDefinition]], [[PlayerId]]

## Usado por
[[NetworkNameTag]]
