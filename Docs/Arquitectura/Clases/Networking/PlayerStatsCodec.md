---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: static
archivo: "Assets/Scripts/Networking/Persistence/PlayerStatsCodec.cs"
tags: [clase, modulo/networking]
---
# PlayerStatsCodec

> Schema of the cloud stats record (key `Key`, a JSON object the Cloud Save SDK serializes): { "v": 1, "matches", "wins", "draws", "losses", "animals", "tackles", "batch" }. Pure: no SDK types. Reading accepts what the SDK's JSON deserialization yields for numbers (long, int, an integral double) and numeric strings; values are sanitized (see `PlayerStatsRules.Sanitize`) and clipped to `int.MaxValue`.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PlayerStatsCodec.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PlayerStatsCodec {
    <<static>>
    +Write(PlayerStatsRecord record, IDictionary~string, object~ target)$ void
    +Read(IReadOnlyDictionary~string, object~ source, PlayerStatsRecord record)$ StatsReadResult
    +TryReadCount(object value, int count)$ bool
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PlayerStatsRecord]], [[PlayerStatsRules]], [[PlayerStatsSummary]], [[StatsReadResult]]

## Usado por
[[CloudSavePlayerStatsStore]], [[PlayerStatsLedger]], [[PlayerStatsRecord]]
