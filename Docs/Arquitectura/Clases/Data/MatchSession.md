---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/MatchSession.cs"
tags: [clase, modulo/data]
---
# MatchSession

> Runtime-only ScriptableObject that carries the players' choices (devices, scheme, character) from the MainMenu to the gameplay scene. Nothing is serialized: data written during a previous play session is discarded through a generation counter bumped on every play start (works without domain reload). Online, `Mode` is `GameMode.Online` and the local player's pick is `LocalCharacter`.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/MatchSession.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class MatchSession {
    <<ScriptableObject>>
    +bool IsReady
    +GameMode Mode
    +CharacterDefinition LocalCharacter
    +SetSlot(SessionSlot slot) void
    +SetMode(GameMode mode) void
    +SetLocalCharacter(CharacterDefinition character) void
    +TryGetSlot(PlayerId id, SessionSlot slot) bool
    +Clear() void
  }
  class CharacterDefinition:::external
  <<ScriptableObject>> CharacterDefinition
  class SessionData:::external
  MatchSession --> CharacterDefinition
  MatchSession --> SessionData
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CharacterDefinition]], [[GameMode]], [[PlayerId]], [[SessionData]], [[SessionSlot]]

## Usado por
[[JoinPromptView]], [[LocalMatchExitHandler]], [[MatchExit]], [[MenuFlowController]], [[PlayerJoinCoordinator]]
