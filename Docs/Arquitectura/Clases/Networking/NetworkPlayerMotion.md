---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: class
archivo: "Assets/Scripts/Networking/Player/NetworkPlayerMotion.cs"
tags: [clase, modulo/networking]
---
# NetworkPlayerMotion

> Player position sync with custom snapshots (no NetworkTransform: later prediction and lag compensation need to know which server time is drawn). Server: after each physics step sends a `PlayerSnapshot` to every client (unreliable). Clients: buffer them and show the pose at ServerTime - InterpolationDelay on a kinematic body, the owner too (no prediction yet). Teleports snap instead of sliding. Runs before input and movement so the snapshot holds the state the last physics step produced.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/NetworkPlayerMotion.cs`

## Hereda / implementa
`NetworkBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkPlayerMotion {
    -NetworkSettings _settings
    -PlayerMovement _movement
    -Rigidbody _rb
    -NetworkPlayerInput _input
    +int BufferedSnapshots
    +OnNetworkSpawn() void
    +MarkTeleport() void
  }
  class NetworkPlayerInput:::external
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class PlayerMovement:::external
  <<MonoBehaviour>> PlayerMovement
  class SnapshotBuffer:::external
  NetworkPlayerMotion --> NetworkPlayerInput
  NetworkPlayerMotion --> NetworkSettings
  NetworkPlayerMotion --> PlayerMovement
  NetworkPlayerMotion --> SnapshotBuffer
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[InterpolatedPose]], [[NetworkPlayerInput]], [[NetworkSettings]], [[PlayerMovement]], [[PlayerSnapshot]], [[PlayerSnapshotFlags]], [[SnapshotAddResult]], [[SnapshotBuffer]], [[SnapshotInterpolator]]

## Usado por
[[NetworkPlayer]]
