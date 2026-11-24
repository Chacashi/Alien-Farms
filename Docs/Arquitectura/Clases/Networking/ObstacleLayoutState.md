---
modulo: Networking
namespace: AlienFarms.Networking.Match
tipo: struct
archivo: "Assets/Scripts/Networking/Match/ObstacleLayoutState.cs"
tags: [clase, modulo/networking]
---
# ObstacleLayoutState

> The obstacle layout of the Animal Zone, one packed slot per `ObstaclePlacement` (slot order): bits 10-15 = variant + 1 (0 = empty slot), bits 0-9 = yaw in 1/1024 turns (~0.35 deg). Fixed size, no allocations.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Match` · **Archivo:** `Assets/Scripts/Networking/Match/ObstacleLayoutState.cs`

## Hereda / implementa
`INetworkSerializable`, `IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ObstacleLayoutState {
    <<struct>>
    +int Count
    +FromPlacements(ObstaclePlacement placements, int count)$ ObstacleLayoutState
    +CopyTo(ObstaclePlacement destination) int
    +Pack(ObstaclePlacement placement)$ ushort
    +Unpack(ushort packed)$ ObstaclePlacement
    +NetworkSerialize(BufferSerializer~T~ serializer) void
    +Equals(ObstacleLayoutState other) bool
    +Equals(object obj) bool
    +GetHashCode() int
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ObstaclePlacement]]

## Usado por
[[NetworkMatchState]]
