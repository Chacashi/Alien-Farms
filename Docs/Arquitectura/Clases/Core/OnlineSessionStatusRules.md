---
modulo: Core
namespace: AlienFarms.Core
tipo: static
archivo: "Assets/Scripts/Core/OnlineSessionStatusRules.cs"
tags: [clase, modulo/core]
---
# OnlineSessionStatusRules

> Allowed transitions of `OnlineSessionStatus`. Pure, so the session channel and its tests share them. Any status may fall back to `OnlineSessionStatus.SignedOut` (services reset or root torn down).

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/OnlineSessionStatusRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class OnlineSessionStatusRules {
    <<static>>
    +CanTransition(OnlineSessionStatus from, OnlineSessionStatus to)$ bool
    +CanStartSession(OnlineSessionStatus status)$ bool
    +CanLeave(OnlineSessionStatus status)$ bool
    +CanSignIn(OnlineSessionStatus status)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[OnlineSessionStatus]]

## Usado por
[[OnlineMenuController]], [[OnlineSessionChannel]]
