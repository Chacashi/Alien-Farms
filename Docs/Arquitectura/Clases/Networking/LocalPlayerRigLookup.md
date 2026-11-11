---
modulo: Networking
namespace: AlienFarms.Networking.Player
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Player/LocalPlayerRigLookup.cs"
tags: [clase, modulo/networking]
---
# LocalPlayerRigLookup

> Online `IPlayerRigLookup` for the shared menus (pause, results): only the avatar this machine owns is found, so a menu is routed to the local player and never touches remote avatars. Lives under OnlineModeRoot and is injected into the PlayerMenuRouter by the GameModeRouter.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Player` · **Archivo:** `Assets/Scripts/Networking/Player/LocalPlayerRigLookup.cs`

## Hereda / implementa
`MonoBehaviour`, [[IPlayerRigLookup]]

## Diagrama
```mermaid
classDiagram
  direction LR
  class LocalPlayerRigLookup {
    <<MonoBehaviour>>
    -NetworkRuntimeAnchor _anchor
    +TryGetRig(PlayerId id, PlayerRig rig) bool
  }
  class IPlayerRigLookup:::external
  <<interface>> IPlayerRigLookup
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  LocalPlayerRigLookup --> NetworkRuntimeAnchor
  LocalPlayerRigLookup ..|> IPlayerRigLookup
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[IPlayerRigLookup]], [[NetworkPlayer]], [[NetworkRuntimeAnchor]], [[PlayerId]], [[PlayerRig]]
