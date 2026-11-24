---
modulo: Networking
namespace: AlienFarms.Networking.Persistence
tipo: class
archivo: "Assets/Scripts/Networking/Persistence/PendingSegmentJson.cs"
tags: [clase, modulo/networking]
---
# PendingSegmentJson

> JsonUtility shape of the tail segment of the local pending stats (`PendingStatsCodec`, v2). Field names are the JSON names, so they deliberately break the _camelCase convention.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Persistence` · **Archivo:** `Assets/Scripts/Networking/Persistence/PendingSegmentJson.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class PendingSegmentJson {
    -string batch
    -int matches
    -int wins
    -int draws
    -int losses
    -int animals
    -int tackles
    +string Batch
    +int Matches
    +int Wins
    +int Draws
    +int Losses
    +int Animals
    +int Tackles
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usado por
[[PendingStatsCodec]], [[PendingStatsJson]]
