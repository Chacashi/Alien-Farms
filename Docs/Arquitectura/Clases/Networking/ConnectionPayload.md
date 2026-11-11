---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: struct
archivo: "Assets/Scripts/Networking/Session/ConnectionPayload.cs"
tags: [clase, modulo/networking]
---
# ConnectionPayload

> What a client sends in `NetworkConfig.ConnectionData` when it connects. Written by the client, read by the server's approval. `UgsPlayerId` is reserved for reconnection (week 2) and not evaluated yet.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ConnectionPayload.cs`

## Hereda / implementa
`INetworkSerializable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ConnectionPayload {
    <<struct>>
    +int ProtocolVersion
    +FixedString32Bytes DisplayName
    +byte CharacterIndex
    +FixedString64Bytes UgsPlayerId
    +Create(int protocolVersion, string displayName, int characterIndex)$ ConnectionPayload
    +bool HasCharacter
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +ToBytes() byte[]
    +TryParse(byte data, ConnectionPayload payload)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[ApprovalRules]], [[ConnectionApprovalHandler]], [[DevConnectionPanel]], [[NetworkPlayer]], [[NetworkPlayerRoster]], [[ServerSlotDirectory]], [[Status]]
