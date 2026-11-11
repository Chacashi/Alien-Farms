---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: struct
archivo: "Assets/Scripts/Networking/Player/InputCommandBatch.cs"
tags: [clase, modulo/networking]
---
# InputCommandBatch

> The last `MaxCommands` commands of a player, newest first, sent in every unreliable input packet so a lost packet is recovered by the next ones. Fixed fields, no arrays: no allocations to build or read it.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/InputCommandBatch.cs`

## Hereda / implementa
`INetworkSerializable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class InputCommandBatch {
    <<struct>>
    +int Count
    +Push(PlayerInputCommand command) void
    +Get(int index) PlayerInputCommand
    +Clear() void
    +NetworkSerialize(BufferSerializer~T~ serializer) void
  }
  class PlayerInputCommand:::external
  <<struct>> PlayerInputCommand
  InputCommandBatch --> PlayerInputCommand
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerInputCommand]]

## Usado por
[[Mode]], [[NetworkPlayerInput]]
