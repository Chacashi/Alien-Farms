---
modulo: Core
namespace: AlienFarms.Core
tipo: interface
archivo: "Assets/Scripts/Core/IStunnable.cs"
tags: [clase, modulo/core]
---
# IStunnable

> Something a blast can stun (a player): drops what it carries, gets knocked back and loses control.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/IStunnable.cs`

## Derivados / implementaciones
[[PlayerStunReceiver]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class IStunnable {
    <<interface>>
    +PlayerId Id
    +ReceiveStun(PlayerId source, Vector3 origin, float duration, float knockbackSpeed) void
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerId]]

## Usado por
[[Bomb]], [[PlayerStunReceiver]]
