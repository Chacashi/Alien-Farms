---
modulo: Networking
namespace: AlienFarms.Networking.Animals
tipo: struct
archivo: "Assets/Scripts/Networking/Animals/AnimalNetState.cs"
tags: [clase, modulo/networking]
---
# AnimalNetState

> Replicated gameplay state of an animal: its `AnimalState`, who carries it, whose pen holds it and why it last left a player's hands (clients raise the same drop GameEvent the server did). 4 bytes.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Animals` · **Archivo:** `Assets/Scripts/Networking/Animals/AnimalNetState.cs`

## Hereda / implementa
`INetworkSerializable`, `IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class AnimalNetState {
    <<struct>>
    +AnimalState State
    +PlayerId Holder
    +PlayerId ZoneOwner
    +DropReason LastDrop
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(AnimalNetState other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[AnimalState]], [[DropReason]], [[PlayerId]]

## Usado por
[[NetworkAnimal]]
