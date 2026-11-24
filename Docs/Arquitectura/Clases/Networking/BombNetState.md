---
modulo: Networking
namespace: AlienFarms.Networking.Items
tipo: struct
archivo: "Assets/Scripts/Networking/Items/BombNetState.cs"
tags: [clase, modulo/networking]
---
# BombNetState

> Replicated gameplay state of a bomb: its `BombState`, who carries it and who threw it. 3 bytes.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Items` · **Archivo:** `Assets/Scripts/Networking/Items/BombNetState.cs`

## Hereda / implementa
`INetworkSerializable`, `IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class BombNetState {
    <<struct>>
    +BombState State
    +PlayerId Holder
    +PlayerId Thrower
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(BombNetState other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[BombState]], [[PlayerId]]

## Usado por
[[NetworkBomb]]
