---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: struct
archivo: "Assets/Scripts/Networking/Player/PlayerInputCommand.cs"
tags: [clase, modulo/networking]
---
# PlayerInputCommand

> One physics step of a player's input, quantized for the wire (11 bytes): sequence, move (sbyte per axis), camera view (yaw as ushort over 360 deg, pitch in centidegrees) and the held buttons as bits.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/PlayerInputCommand.cs`

## Hereda / implementa
`INetworkSerializable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerInputCommand {
    <<struct>>
    +uint Seq
    +sbyte MoveX
    +sbyte MoveY
    +ushort YawQuantized
    +short PitchCentidegrees
    +byte Buttons
    +Create(uint seq, Vector2 move, float yaw, float pitch, bool sprint, bool grab, bool aim)$ PlayerInputCommand
    +Vector2 Move
    +float Yaw
    +float Pitch
    +bool IsSprintHeld
    +bool IsGrabHeld
    +bool IsAimHeld
    +NetworkSerialize(BufferSerializer~T~ serializer) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[InputCommandBatch]], [[InputCommandBuffer]], [[Mode]], [[NetworkCommandSource]], [[NetworkPlayerInput]]
