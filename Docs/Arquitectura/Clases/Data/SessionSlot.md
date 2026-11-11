---
modulo: Data
namespace: AlienFarms.Data
tipo: struct
archivo: "Assets/Scripts/Data/SessionSlot.cs"
tags: [clase, modulo/data]
---
# SessionSlot

> What one player chose before the match: its control scheme, the devices it used (both keyboard and mouse for KeyboardMouse) and its character. Devices may be null or empty (the player then joins by pressing a button).

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/SessionSlot.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SessionSlot {
    <<struct>>
    +PlayerId Id
    +string ControlScheme
    +InputDevice[] Devices
    +CharacterDefinition Character
    +bool IsValid
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  SessionSlot --> CharacterDefinition
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[PlayerId]]

## Usado por
[[MatchSession]], [[MenuFlowController]], [[PlayerJoinCoordinator]], [[SessionData]]
