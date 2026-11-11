---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Session/ConnectionApprovalHandler.cs"
tags: [clase, modulo/networking]
---
# ConnectionApprovalHandler

> Server connection approval (version 1.5): parses the `ConnectionPayload`, applies `ApprovalRules` and seats the client in the `ServerSlotDirectory` (Player1, then Player2). No player object is created by NGO: the gameplay scene's roster spawns it. Also runs for the host's own client.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/ConnectionApprovalHandler.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class ConnectionApprovalHandler {
    <<MonoBehaviour>>
    -NetworkSettings _settings
    -NetworkManager _manager
    +ServerSlotDirectory Slots
  }
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class ServerSlotDirectory:::external
  ConnectionApprovalHandler --> NetworkSettings
  ConnectionApprovalHandler --> ServerSlotDirectory
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[ApprovalDecision]], [[ApprovalRules]], [[ConnectionPayload]], [[NetworkSettings]], [[PlayerId]], [[PlayerSlot]], [[ServerSlotDirectory]]

## Usado por
[[NetworkRootLifetime]], [[NetworkRuntimeAnchor]]
