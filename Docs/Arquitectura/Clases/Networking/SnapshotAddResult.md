---
modulo: Networking
namespace: AlienFarms.Networking.Prediction
tipo: enumeration
archivo: "Assets/Scripts/Networking/Prediction/SnapshotAddResult.cs"
tags: [clase, modulo/networking]
---
# SnapshotAddResult

> What `SnapshotBuffer.Add` did with a snapshot.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Prediction` · **Archivo:** `Assets/Scripts/Networking/Prediction/SnapshotAddResult.cs`

## Diagrama
```mermaid
classDiagram
  direction LR
  class SnapshotAddResult {
    <<enumeration>>
    Rejected
    Added
    Reset
  }
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Valores
`Rejected`, `Added`, `Reset`

## Usado por
[[NetworkPlayerMotion]], [[SnapshotBuffer]]
