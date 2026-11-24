---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: struct
archivo: "Assets/Scripts/Networking/Session/LobbySlotEntry.cs"
tags: [clase, modulo/networking]
---
# LobbySlotEntry

> One seat of the online room as replicated by `NetworkLobbyState`. Element of a NetworkList, so it is unmanaged and equatable. `CharacterIndex` is `PlayerSlot.NoCharacter` while the seat has no pick; picking counts as ready.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/LobbySlotEntry.cs`

## Hereda / implementa
`INetworkSerializable`, `IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LobbySlotEntry {
    <<struct>>
    +PlayerId Id
    +ulong ClientId
    +byte CharacterIndex
    +FixedString32Bytes DisplayName
    +bool HasCharacter
    +WithCharacter(byte characterIndex) LobbySlotEntry
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(LobbySlotEntry other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]], [[PlayerSlot]]

## Usado por
[[LobbyRules]], [[NetworkLobbyState]]
