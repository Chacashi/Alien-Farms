---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: struct
archivo: "Assets/Scripts/Networking/Match/PenEntry.cs"
tags: [clase, modulo/networking]
---
# PenEntry

> One line of a player's pen ledger: how many animals of a type (index in the spawn settings' entries) the pen holds. Element of a NetworkList, so it is unmanaged and equatable.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/PenEntry.cs`

## Hereda / implementa
`INetworkSerializable`, `IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PenEntry {
    <<struct>>
    +int AnimalTypeIndex
    +int Count
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(PenEntry other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[NetworkPenLedger]]
