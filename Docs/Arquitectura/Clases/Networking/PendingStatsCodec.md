---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: static
archivo: "Assets/Scripts/Networking/Persistence/PendingStatsCodec.cs"
tags: [clase, modulo/networking]
---
# PendingStatsCodec

> JSON of the local pending stats (v2): { "v":2, "batch":"…", "matches", "wins", "draws", "losses", "animals", "tackles", "sent":bool, "tail":{ "batch", "matches", … } }. The top level is the head (a v1 entry, without "sent" or "tail", reads as an unsent head). Pure (JsonUtility only), so it is tested in EditMode. Called on enqueue and load only, never per frame.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PendingStatsCodec.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PendingStatsCodec {
    <<static>>
    +Write(PendingStats pending)$ string
    +Read(string json, PendingStats pending)$ StatsReadResult
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PendingSegmentJson]], [[PendingStats]], [[PendingStatsJson]], [[PlayerStatsRules]], [[PlayerStatsSummary]], [[StatsReadResult]]

## Usado por
[[PlayerPrefsPendingStatsStore]]
