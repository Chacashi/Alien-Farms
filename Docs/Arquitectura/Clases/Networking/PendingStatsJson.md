---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: class
archivo: "Assets/Scripts/Networking/Persistence/PendingStatsJson.cs"
tags: [clase, modulo/networking]
---
# PendingStatsJson

> JsonUtility shape of the local pending stats (`PendingStatsCodec`): the head at the top level (what a v1 entry holds), plus sent and the tail segment (v2). Field names are the JSON names, so they deliberately break the _camelCase convention.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PendingStatsJson.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PendingStatsJson {
    -int v
    -string batch
    -int matches
    -int wins
    -int draws
    -int losses
    -int animals
    -int tackles
    -bool sent
    -PendingSegmentJson tail
    +int Version
    +string Batch
    +int Matches
    ...7 más
  }
  class PendingSegmentJson:::external
  PendingStatsJson --> PendingSegmentJson
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[PendingSegmentJson]]

## Usado por
[[PendingStatsCodec]]
