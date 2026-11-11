---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: enumeration
archivo: "Assets/Scripts/Networking/Player/PlayerSnapshotFlags.cs"
tags: [clase, modulo/networking]
---
# PlayerSnapshotFlags

> Bit flags of a `PlayerSnapshot` (teleports travel as `PlayerSnapshot.TeleportEpoch`).

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/PlayerSnapshotFlags.cs`

## Hereda / implementa
`byte`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerSnapshotFlags {
    <<enumeration>>
    None
    Sprinting
    Aiming
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`None`, `Sprinting`, `Aiming`

## Usado por
[[InterpolatedPose]], [[NetworkPlayerMotion]], [[PlayerSnapshot]]
