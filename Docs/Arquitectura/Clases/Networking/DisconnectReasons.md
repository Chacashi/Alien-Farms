---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: static
archivo: "Assets/Scripts/Networking/Session/DisconnectReasons.cs"
tags: [clase, modulo/networking]
---
# DisconnectReasons

> The only disconnect / rejection reasons the game sends, as stable keys the UI maps to text. NGO fills DisconnectReason with raw transport text when the server gave none, so clients show only these keys (`Normalize`).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/DisconnectReasons.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class DisconnectReasons {
    <<static>>
    +IsKnown(string reason)$ bool
    +Normalize(string reason)$ string
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ApprovalRules]], [[NetworkRootLifetime]], [[OnlineMatchBootstrap]]
