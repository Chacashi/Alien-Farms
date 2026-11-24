---
modulo: Networking
namespace: AlienFarms.Networking.Session
tipo: static
archivo: "Assets/Scripts/Networking/Session/LobbyRules.cs"
tags: [clase, modulo/networking]
---
# LobbyRules

> Pure rules of the online room, applied by the server on a small mirror of the replicated seats (never on the NetworkList itself). Requests are processed one at a time, so when two players ask for the same character the first one wins and the second gets `LobbyRequestResult.RejectedTaken`.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Session` · **Archivo:** `Assets/Scripts/Networking/Session/LobbyRules.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class LobbyRules {
    <<static>>
    +EvaluateRequest(LobbySlotEntry entries, int count, ulong senderClientId)$ LobbyRequestResult
    +ApplyRequest(LobbySlotEntry entries, int count, ulong senderClientId)$ LobbyRequestResult
    +IsTaken(LobbySlotEntry entries, int count, byte index, PlayerId except)$ bool
    +CanStart(LobbySlotEntry entries, int count, int requiredSeats)$ bool
    +Reconcile(LobbySlotEntry current, int count, ReadOnlySpan~PlayerSlot~ seats, LobbySlotEntry output)$ int
    +ToErrorKey(LobbyRequestResult result)$ string
    +IsRejected(LobbyRequestResult result)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[LobbyRequestResult]], [[LobbySlotEntry]], [[OnlineSessionErrorKeys]], [[PlayerId]], [[PlayerSlot]]

## Usado por
[[NetworkLobbyState]]
