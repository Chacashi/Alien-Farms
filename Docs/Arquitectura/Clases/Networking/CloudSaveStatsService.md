---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: MonoBehaviour
archivo: "Assets/Scripts/Networking/Persistence/CloudSaveStatsService.cs"
tags: [clase, modulo/networking]
---
# CloudSaveStatsService

> Cloud stats service on the persistent NetworkRoot (task 2.8). Composition root of `PlayerStatsSync`: the anchor's `NetworkRuntimeAnchor.PlayerStats` ledger, Cloud Save (behind the simulated-failure decorator), the online session channel (UGS id, participants) and the progress channel it is the only writer of. Loads when the UGS id changes, saves whenever a round is pending, retries with backoff (cancelled in `OnDisable`) and reads the rival in the room. Event driven: no Update.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/CloudSaveStatsService.cs`

## Hereda / implementa
`MonoBehaviour`

## Diagrama
```mermaid
classDiagram
  direction LR
  class CloudSaveStatsService {
    <<MonoBehaviour>>
    -NetworkSettings _settings
    -OnlineSessionChannel _session
    -PlayerProgressChannel _progress
    -NetworkRuntimeAnchor _anchor
  }
  class NetworkRuntimeAnchor:::external
  <<ScriptableObject>> NetworkRuntimeAnchor
  class NetworkSettings:::external
  <<ScriptableObject>> NetworkSettings
  class OnlineSessionChannel:::external
  <<ScriptableObject>> OnlineSessionChannel
  class PlayerProgressChannel:::external
  <<ScriptableObject>> PlayerProgressChannel
  class PlayerStatsSync:::external
  CloudSaveStatsService --> NetworkRuntimeAnchor
  CloudSaveStatsService --> NetworkSettings
  CloudSaveStatsService --> OnlineSessionChannel
  CloudSaveStatsService --> PlayerProgressChannel
  CloudSaveStatsService --> PlayerStatsSync
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[CloudSavePlayerStatsStore]], [[IPlayerStatsStore]], [[NetworkRuntimeAnchor]], [[NetworkSettings]], [[OnlineSessionChannel]], [[PlayerProgressChannel]], [[PlayerStatsSync]], [[SimulatedFailureStatsStore]]
