---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/MatchSession.cs"
tags: [clase, modulo/data]
---
# MatchSession

> Runtime-only ScriptableObject that carries the players' choices (devices, scheme, character) from the MainMenu to the gameplay scene. Nothing is serialized: data written during a previous play session is discarded through a generation counter bumped on every play start (works without domain reload). Online, `Mode` is `GameMode.Online` (characters are picked in the online room, server side).

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
    +bool ReturnToOnlineMenu
    +SetSlot(SessionSlot slot) void
    +SetMode(GameMode mode) void
    +RequestReturnToOnlineMenu() void
    +ConsumeReturnToOnlineMenu() bool
    +TryGetSlot(PlayerId id, SessionSlot slot) bool
    +Clear() void
  }
  class SessionData:::external
  MatchSession --> SessionData
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[GameMode]], [[Mode]], [[PlayerId]], [[SessionData]], [[SessionSlot]]

## Usado por
[[DevConnectionPanel]], [[GameModeRouter]], [[JoinPromptView]], [[LocalMatchExitHandler]], [[MatchExit]], [[MenuFlowController]], [[OnlineMatchBootstrap]], [[OnlineMatchExitHandler]], [[OnlineSessionService]], [[PlayerJoinCoordinator]], [[SaveStatusBadgeView]], [[SessionExit]], [[Status]]
