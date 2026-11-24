---
modulo: Core
namespace: AlienFarms.Core
tipo: enumeration
archivo: "Assets/Scripts/Core/DropReason.cs"
tags: [clase, modulo/core]
---
# DropReason

> Why a carried `IGrabbable` left its holder.

**Módulo:** [[Modulo Core|Core]] · **Namespace:** `AlienFarms.Core` · **Archivo:** `Assets/Scripts/Core/DropReason.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class DropReason {
    <<enumeration>>
    Released
    Stolen
    Forced
    Thrown
    Stunned
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Released`, `Stolen`, `Forced`, `Thrown`, `Stunned`

## Usado por
[[Animal]], [[AnimalNetState]], [[AnimalSfxPlayer]], [[Bomb]], [[GameEvents]], [[IGrabbable]], [[Mode]], [[NetworkAnimal]], [[NetworkCarry]], [[NetworkPlayerRoster]], [[PlayerGrabber]], [[PlayerRig]], [[PlayerStunReceiver]], [[PlayerTackle]]
