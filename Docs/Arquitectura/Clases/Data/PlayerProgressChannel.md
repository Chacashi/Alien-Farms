---
modulo: Data
namespace: AlienFarms.Data
tipo: ScriptableObject
archivo: "Assets/Scripts/Data/PlayerProgressChannel.cs"
tags: [clase, modulo/data]
---
# PlayerProgressChannel

> Runtime channel with the local player's persistent progress (lifetime stats) and, in the online room, the rival's public stats. Views only read it; the cloud save service (task 2.8) is the only writer. Until then it stays `PlayerProgressStatus.Unavailable`. Nothing is serialized: state from a previous play session is discarded through a generation counter.

**Módulo:** [[Modulo Data|Data]] · **Namespace:** `AlienFarms.Data` · **Archivo:** `Assets/Scripts/Data/PlayerProgressChannel.cs`

## Hereda / implementa
`ScriptableObject`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerProgressChannel {
    <<ScriptableObject>>
    +event Action Changed
    +event Action HostLeftWinNoticeConsumeRequested
    +PlayerProgressStatus Status
    +PlayerStatsSummary Stats
    +bool HasStats
    +PlayerProgressSaveStatus SaveStatus
    +string LastErrorKey
    +bool HasError
    +PlayerProgressStatus RivalStatus
    +PlayerStatsSummary RivalStats
    +bool HasRivalStats
    +bool HasHostLeftWinNotice
    +SetUnavailable() void
    ...11 más
  }
  class PlayerStatsSummary:::external
  <<struct>> PlayerStatsSummary
  PlayerProgressChannel --> PlayerStatsSummary
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerProgressSaveStatus]], [[PlayerProgressStatus]], [[PlayerStatsSummary]], [[Status]]

## Usado por
[[CloudSaveStatsService]], [[OnlineMenuController]], [[PlayerStatsSync]], [[RivalStatsView]], [[SaveStatusBadgeView]], [[TitleStatsView]]
