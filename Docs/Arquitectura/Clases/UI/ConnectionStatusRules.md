---
modulo: UI
namespace: AlienFarms.UI
tipo: static
archivo: "Assets/Scripts/UI/ConnectionStatusRules.cs"
tags: [clase, modulo/ui]
---
# ConnectionStatusRules

> Pure rules of `ConnectionStatusView`: which banner a seat's `PlayerConnectionStatus` shows, which localization key it uses and how the grace countdown is displayed. No allocation.

**Módulo:** [[Modulo UI|UI]] · **Namespace:** `AlienFarms.UI` · **Archivo:** `Assets/Scripts/UI/ConnectionStatusRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ConnectionStatusRules {
    <<static>>
    +Resolve(PlayerConnectionStatus status, float graceLeft, bool wasReconnecting)$ ConnectionBanner
    +IsToast(ConnectionBanner banner)$ bool
    +Key(ConnectionBanner banner)$ string
    +DisplaySeconds(float graceLeft)$ int
    +IsAwaiting(PlayerConnectionStatus status)$ bool
    +EscapeForTemplate(string text)$ string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ConnectionBanner]], [[LocalizationKeys]], [[PlayerConnectionStatus]]

## Usado por
[[ConnectionStatusView]]
