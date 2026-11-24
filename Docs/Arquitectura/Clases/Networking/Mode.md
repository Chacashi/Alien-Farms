---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: enumeration
archivo: "Assets/Scripts/Networking/Player/NetworkPlayerInput.cs"
tags: [clase, modulo/networking]
---
# Mode

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/NetworkPlayerInput.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class Mode {
    <<enumeration>>
    Idle
    OwnerSender
    ServerReceiver
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Idle`, `OwnerSender`, `ServerReceiver`

## Usa
[[IPlayerCommandSource]], [[InputCommandBatch]], [[InputCommandBuffer]], [[NetworkCommandSource]], [[NetworkSettings]], [[PlayerAim]], [[PlayerGrabber]], [[PlayerInputCommand]], [[PlayerInputHandler]], [[PlayerMovement]], [[ThirdPersonCamera]]

## Usado por
[[GameModeRouter]], [[JoinPromptView]], [[LanguageSelector]], [[MatchSession]], [[NetworkCarry]], [[NetworkPlayerInput]], [[OnlinePanelView]], [[SaveStatusBadgeView]]
