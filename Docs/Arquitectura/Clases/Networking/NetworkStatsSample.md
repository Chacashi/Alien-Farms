---
modulo: Networking
namespace: AlienFarms.Networking.Diagnostics
tipo: struct
archivo: "Assets/Scripts/Networking/Diagnostics/NetworkStatsSample.cs"
tags: [clase, modulo/networking]
---
# NetworkStatsSample

> One reading of the online debug stats view: pool counters of this machine and, when a transport is running, its receive side (packets per second, receive-queue usage) and the longest frame since the session started. Compared with the previous reading so the label is rebuilt only when a number changes.

**Módulo:** [[Modulo Networking|Networking]] · **Namespace:** `AlienFarms.Networking.Diagnostics` · **Archivo:** `Assets/Scripts/Networking/Diagnostics/NetworkStatsSample.cs`

## Hereda / implementa
`IEquatable`

## Diagrama
```mermaid
classDiagram
  direction LR
  class NetworkStatsSample {
    <<struct>>
    +NetworkPoolStats Animals
    +NetworkPoolStats Bombs
    +bool HasTransport
    +int RxPacketsPerSecond
    +int RxQueueMeanTenths
    +int RxQueueMax
    +int QueueCapacity
    +int LongestFrameMs
    +Equals(NetworkStatsSample other) bool
    +Equals(object obj) bool
    +GetHashCode() int
    +bool operator$
  }
  class NetworkPoolStats:::external
  <<struct>> NetworkPoolStats
  NetworkStatsSample --> NetworkPoolStats
  classDef external fill:#eee,stroke:#999,stroke-dasharray: 4 3,color:#555
```

## Usa
[[NetworkPoolStats]]

## Usado por
[[NetworkStatsFormatter]], [[PoolStatsView]]
