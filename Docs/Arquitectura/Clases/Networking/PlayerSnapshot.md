---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: struct
archivo: "Assets/Scripts/Networking/Player/PlayerSnapshot.cs"
tags: [clase, modulo/networking]
---
# PlayerSnapshot

> Authoritative state of one player after a server physics step, sent unreliably every step. `ServerTime` is on the NGO server clock, so clients render it at ServerTime - InterpolationDelay.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/PlayerSnapshot.cs`

## Hereda / implementa
`INetworkSerializable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerSnapshot {
    <<struct>>
    +uint Tick
    +double ServerTime
    +Vector3 Position
    +Vector3 Velocity
    +float FacingYaw
    +float StunRemaining
    +uint AckSeq
    +byte TeleportEpoch
    +PlayerSnapshotFlags Flags
    +NetworkSerialize(BufferSerializer~T~ serializer) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerSnapshotFlags]]

## Usado por
[[NetworkPlayerMotion]], [[SnapshotBuffer]], [[SnapshotInterpolator]]
