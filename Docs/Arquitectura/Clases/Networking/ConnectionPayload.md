---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: struct
archivo: "Assets/Scripts/Networking/Session/ConnectionPayload.cs"
tags: [clase, modulo/networking]
---
# ConnectionPayload

> What a client sends in `NetworkConfig.ConnectionData` when it connects. Written by the client, read by the server's approval (`ApprovalRules`): `UgsPlayerId` identifies the player across reconnections (empty for a direct dev connection without Unity Gaming Services). The character is picked inside the online room (`NetworkLobbyState`), so it is not part of the payload (removed in protocol 2).

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
    +FixedString64Bytes UgsPlayerId
    +Create(int protocolVersion, string displayName, string ugsPlayerId)$ ConnectionPayload
    +bool HasUgsPlayerId
    +WithSanitizedName() ConnectionPayload
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +ToBytes() byte[]
    +TryParse(byte data, ConnectionPayload payload)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[DisplayNameSanitizer]]

## Usado por
[[ApprovalRules]], [[ConnectionApprovalHandler]], [[DevConnectionPanel]], [[OnlineSessionService]], [[ServerSlotDirectory]], [[SessionExit]], [[Status]]
