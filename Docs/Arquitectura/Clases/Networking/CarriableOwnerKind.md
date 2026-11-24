---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: enumeration
archivo: "Assets/Scripts/Networking/Player/CarriableOwnerKind.cs"
tags: [clase, modulo/networking]
---
# CarriableOwnerKind

> Who a carriable object belongs to, as shown by its ownership tint and debug label.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/CarriableOwnerKind.cs`

## Hereda / implementa
`byte`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CarriableOwnerKind {
    <<enumeration>>
    Server
    Player
    Unresolved
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Server`, `Player`, `Unresolved`

## Usado por
[[CarriableOwner]], [[CarriableOwnerRules]], [[CarriableOwnerTint]], [[CarriableOwnershipLabel]]
